using System;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.Helpers;


namespace FFImageLoading.Cache
{
    public class SimpleDiskCache : IDiskCache
    {
        readonly SemaphoreSlim fileWriteLock = new SemaphoreSlim(1, 1);
        readonly SemaphoreSlim _currentWriteLock = new SemaphoreSlim(1, 1);
        Task initTask = null;
        string cacheFolderName;
        IsolatedStorageFile rootFolder;
        IsolatedStorageFile cacheFolder;
        ConcurrentDictionary<string, byte> fileWritePendingTasks = new ConcurrentDictionary<string, byte>();
        ConcurrentDictionary<string, CacheEntry> entries = new ConcurrentDictionary<string, CacheEntry>();
        Task _currentWrite = Task.FromResult<byte>(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleDiskCache"/> class. This constructor attempts
        /// to create a folder of the given name under the <see cref="ApplicationData.TemporaryFolder"/>.
        /// </summary>
        /// <param name="cacheFolderName">The name of the cache folder.</param>
        /// <param name="configuration">The configuration object.</param>
        public SimpleDiskCache(string cacheFolderName, Configuration configuration)
        {
            Configuration = configuration;
            this.cacheFolderName = cacheFolderName;
            initTask = Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleDiskCache"/> class. This constructor attempts
        /// to create a folder of the given name under the given root <see cref="StorageFolder"/>.
        /// </summary>
        /// <param name="rootFolder">The root folder where the cache folder will be created.</param>
        /// <param name="cacheFolderName">The cache folder name.</param>
        /// <param name="configuration">The configuration object.</param>
        public SimpleDiskCache(IsolatedStorageFile rootFolder, string cacheFolderName, Configuration configuration)
        {
            Configuration = configuration;
            this.rootFolder = rootFolder ?? IsolatedStorageFile.GetUserStoreForApplication();
            this.cacheFolderName = cacheFolderName;
            initTask = Init();
        }

        protected Configuration Configuration { get; private set; }
        protected IMiniLogger Logger { get { return Configuration.Logger; } }

        protected virtual async Task Init()
        {
            try
            {
                CreateCacheDirIfNotExists();
                await InitializeEntries().ConfigureAwait(false);
            }
            catch
            {
                rootFolder.DeleteDirectory(cacheFolderName);
                CreateCacheDirIfNotExists();
            }
            finally
            {
                var task = CleanCallback();
            }
        }

        private void CreateCacheDirIfNotExists()
        {
            if (!rootFolder.DirectoryExists(cacheFolderName))
                rootFolder.CreateDirectory(cacheFolderName);
        }

        protected virtual async Task InitializeEntries()
        {
            foreach (var file in GetAllEntries())
            {
                var name = Path.GetFileName(file);
                var key = Path.GetFileNameWithoutExtension(file);
                var ext = Path.GetExtension(file);
                var duration = GetDuration(ext);
                var created = rootFolder.GetCreationTime(file);
                entries.TryAdd(key, new CacheEntry(created.UtcDateTime, duration, name));
            }
        }

        protected virtual TimeSpan GetDuration(string text)
        {
            string textToParse = text.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(textToParse))
                return Configuration.DiskCacheDuration;

            int duration;
            return int.TryParse(textToParse, out duration) ? TimeSpan.FromSeconds(duration) : Configuration.DiskCacheDuration;
        }

        protected virtual async Task CleanCallback()
        {
            var now = DateTime.UtcNow;
            var kvps = entries.Where(kvp => kvp.Value.Origin + kvp.Value.TimeToLive < now).ToArray();

            foreach (var kvp in kvps)
            {
                if (entries.TryRemove(kvp.Key, out var oldCacheEntry))
                {
                    try
                    {
                        Logger.Debug(string.Format("SimpleDiskCache: Removing expired file {0}", kvp.Key));
                        rootFolder.DeleteFile(GetCacheFileName(oldCacheEntry.FileName));
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// GetFilePath
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual async Task<string> GetFilePathAsync(string key)
        {
            await initTask.ConfigureAwait(false);

            CacheEntry entry;
            if (!entries.TryGetValue(key, out entry))
                return null;

            return Path.Combine(rootFolder.ToString(), GetCacheFileName(entry.FileName));
        }

        /// <summary>
        /// Checks if cache entry exists/
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="key">Key.</param>
        public virtual async Task<bool> ExistsAsync(string key)
        {
            await initTask.ConfigureAwait(false);

            return entries.ContainsKey(key);
        }

        /// <summary>
        /// Adds the file to cache and file saving queue if not exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="bytes">Bytes.</param>
        /// <param name="duration">Duration.</param>
        public virtual async Task AddToSavingQueueIfNotExistsAsync(string key, byte[] bytes, TimeSpan duration, Action writeFinished = null)
        {
            await initTask.ConfigureAwait(false);

            if (!fileWritePendingTasks.TryAdd(key, 1))
                return;

            await _currentWriteLock.WaitAsync().ConfigureAwait(false); // Make sure we don't add multiple continuations to the same task

            try
            {
                _currentWrite = _currentWrite.ContinueWith(async t =>
                {
                    await Task.Yield(); // forces it to be scheduled for later

                    await initTask.ConfigureAwait(false);

                    try
                    {
                        await fileWriteLock.WaitAsync().ConfigureAwait(false);

                        CreateCacheDirIfNotExists();
                        string filename = key + "." + (long)duration.TotalSeconds;

                        var file = rootFolder.CreateFile(GetCacheFileName(filename));

                        //using (var fs = await file.OpenStreamForWriteAsync().ConfigureAwait(false))
                        {
                            await file.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                        }

                        entries[key] = new CacheEntry(DateTime.UtcNow, duration, filename);
                        writeFinished?.Invoke();
                    }
                    catch (Exception ex) // Since we don't observe the task (it's not awaited, we should catch all exceptions)
                    {
                        //TODO WinRT doesn't have Console
                        System.Diagnostics.Debug.WriteLine(string.Format("An error occured while caching to disk image '{0}'.", key));
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        byte finishedTask;
                        fileWritePendingTasks.TryRemove(key, out finishedTask);
                        fileWriteLock.Release();
                    }
                });
            }
            finally
            {
                _currentWriteLock.Release();
            }
        }

        /// <summary>
        /// Tries to get cached file as stream.
        /// </summary>
        /// <returns>The get async.</returns>
        /// <param name="key">Key.</param>
        public virtual async Task<Stream> TryGetStreamAsync(string key)
        {
            await initTask.ConfigureAwait(false);

            await WaitForPendingWriteIfExists(key).ConfigureAwait(false);

            try
            {
                CacheEntry entry;
                if (!entries.TryGetValue(key, out entry))
                    return null;
                
                try
                {
                    return GetFileStream(key);
                }
                catch (IOException)
                {
                    CreateCacheDirIfNotExists();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private Stream GetFileStream(string key)
        {
            var file = GetCacheFileName(key);
            if (!rootFolder.FileExists(file))
            {
                return null;
            }

            return rootFolder.OpenFile(file, FileMode.Create, FileAccess.Read);
        }

        private string GetCacheFileName(string file)
        {
            return $"{cacheFolderName}/{file}";
        }
        /// <summary>
        /// Removes the specified cache entry.
        /// </summary>
        /// <param name="key">Key.</param>
        public virtual async Task RemoveAsync(string key)
        {
            await initTask.ConfigureAwait(false);

            await WaitForPendingWriteIfExists(key).ConfigureAwait(false);

            CacheEntry oldCacheEntry;
            if (entries.TryRemove(key, out oldCacheEntry))
            {
                try
                {
                    var file = GetCacheFileName(key);
                    if (rootFolder.FileExists(file))
                    {
                        rootFolder.DeleteFile(file);
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Clears all cache entries.
        /// </summary>
        public virtual async Task ClearAsync()
        {
            await initTask.ConfigureAwait(false);

            while (fileWritePendingTasks.Count != 0)
            {
                await Task.Delay(20).ConfigureAwait(false);
            }

            try
            {
                await fileWriteLock.WaitAsync().ConfigureAwait(false);

                //var entriesToRemove = rootFolder.GetFileNames($"{cacheFolderName}/*");
                //foreach (var item in entriesToRemove)
                {
                    try
                    {
                        rootFolder.DeleteDirectory(cacheFolderName);
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }
            }
            catch (IOException)
            {
                CreateCacheDirIfNotExists();
            }
            finally
            {
                entries.Clear();
                fileWriteLock.Release();
            }
        }

        private string[] GetAllEntries()
        {
            return rootFolder.GetFileNames($"{cacheFolderName}/*");
        }

        protected virtual async Task WaitForPendingWriteIfExists(string key)
        {
            while (fileWritePendingTasks.ContainsKey(key))
            {
                await Task.Delay(20).ConfigureAwait(false);
            }
        }
    }
}
