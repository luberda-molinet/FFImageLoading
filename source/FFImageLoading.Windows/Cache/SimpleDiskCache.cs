using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using FFImageLoading.Cache;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using FFImageLoading.Config;
using FFImageLoading.Helpers;

#if SILVERLIGHT
using FFImageLoading.Concurrency;
#else
using System.Collections.Concurrent;
#endif


namespace FFImageLoading.Cache
{
    public class SimpleDiskCache : IDiskCache
    {
        readonly SemaphoreSlim fileWriteLock = new SemaphoreSlim(1, 1);
        readonly SemaphoreSlim _currentWriteLock = new SemaphoreSlim(1, 1);
        Task initTask = null;
        string cacheFolderName;
        StorageFolder rootFolder;
        StorageFolder cacheFolder;
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
        public SimpleDiskCache(StorageFolder rootFolder, string cacheFolderName, Configuration configuration)
        {
            Configuration = configuration;
            this.rootFolder = rootFolder ?? ApplicationData.Current.TemporaryFolder;
            this.cacheFolderName = cacheFolderName;
            initTask = Init();
        }

        protected Configuration Configuration { get; private set; }
        protected IMiniLogger Logger { get { return Configuration.Logger; } }

        protected virtual async Task Init()
        {
            try
            {
                cacheFolder = await rootFolder.CreateFolderAsync(cacheFolderName, CreationCollisionOption.OpenIfExists);
                await InitializeEntries().ConfigureAwait(false);
            }
            catch
            {
                StorageFolder folder = null;

                try
                {
                    folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(cacheFolderName);
                }
                catch (Exception)
                {
                }

                if (folder != null)
                {
                    await folder.DeleteAsync();
                    await ApplicationData.Current.LocalFolder.CreateFolderAsync(cacheFolderName, CreationCollisionOption.ReplaceExisting);
                }
            }
            finally
            {
                var task = CleanCallback();
            }
        }

        protected virtual async Task InitializeEntries()
        {
            foreach (var file in await cacheFolder.GetFilesAsync())
            {
                string key = Path.GetFileNameWithoutExtension(file.Name);
                var duration = GetDuration(file.FileType);
                entries.TryAdd(key, new CacheEntry(file.DateCreated.UtcDateTime, duration, file.Name));
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
            KeyValuePair<string, CacheEntry>[] kvps;
            var now = DateTime.UtcNow;
            kvps = entries.Where(kvp => kvp.Value.Origin + kvp.Value.TimeToLive < now).ToArray();

            foreach (var kvp in kvps)
            {
                CacheEntry oldCacheEntry;
                if (entries.TryRemove(kvp.Key, out oldCacheEntry))
                {
                    try
                    {
                        Logger.Debug(string.Format("SimpleDiskCache: Removing expired file {0}", oldCacheEntry.FileName));
                        var file = await cacheFolder.GetFileAsync(oldCacheEntry.FileName);
                        await file.DeleteAsync();
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

            return Path.Combine(cacheFolder.Path, entry.FileName);
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

                        cacheFolder = await rootFolder.CreateFolderAsync(cacheFolderName, CreationCollisionOption.OpenIfExists);
                        string filename = key + "." + (long)duration.TotalSeconds;

                        var file = await cacheFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                        using (var fs = await file.OpenStreamForWriteAsync().ConfigureAwait(false))
                        {
                            await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
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

                StorageFile file = null;

                try
                {
                    file = await cacheFolder.GetFileAsync(entry.FileName);
                }
                catch (IOException)
                {
                    cacheFolder = await rootFolder.CreateFolderAsync(cacheFolderName, CreationCollisionOption.OpenIfExists);
                }

                if (file == null)
                    return null;

                return await file.OpenStreamForReadAsync().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
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
                    var file = await cacheFolder.GetFileAsync(oldCacheEntry.FileName);
                    await file.DeleteAsync();
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

                var entriesToRemove = await cacheFolder.GetFilesAsync();
                foreach (var item in entriesToRemove)
                {
                    try
                    {
                        await item.DeleteAsync();
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }
            }
            catch (IOException)
            {
                cacheFolder = await rootFolder.CreateFolderAsync(cacheFolderName, CreationCollisionOption.OpenIfExists);
            }
            finally
            {
                entries.Clear();
                fileWriteLock.Release();
            }
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
