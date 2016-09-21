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

#if SILVERLIGHT
using FFImageLoading.Concurrency;
#else
using System.Collections.Concurrent;
#endif


namespace FFImageLoading.Cache
{
    public class SimpleDiskCache : IDiskCache
    {
        private static readonly SemaphoreSlim fileWriteLock = new SemaphoreSlim(1, 1);

        Task initTask = null;
        string version;
        string cacheFolderName;
        StorageFolder cacheFolder;
        ConcurrentDictionary<string, byte> fileWritePendingTasks; // we use it as an Hashset, since there's no ConcurrentHashset
        ConcurrentDictionary<string, CacheEntry> entries;
        readonly TimeSpan defaultDuration;
        private readonly SemaphoreSlim _currentWriteLock;
        private Task _currentWrite;

        public SimpleDiskCache(string cacheFolderName)
        {
            this.entries = new ConcurrentDictionary<string, CacheEntry>();
            this.cacheFolder = null;
            this.cacheFolderName = cacheFolderName;
            this.fileWritePendingTasks = new ConcurrentDictionary<string, byte>();
            defaultDuration = new TimeSpan(30, 0, 0, 0);  // the default is 30 days
            _currentWriteLock = new SemaphoreSlim(1, 1);
            _currentWrite = Task.FromResult<byte>(1);

            initTask = Init();
        }

        /// <summary>
        /// Creates new cache default instance.
        /// </summary>
        /// <returns>The cache.</returns>
        /// <param name="cacheName">Cache name.</param>
        /// <param name="version">Version.</param>
        public static SimpleDiskCache CreateCache(string cacheName)
        {
            return new SimpleDiskCache(cacheName);
        }

        async Task Init()
        {
            try
            {
                cacheFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(cacheFolderName, CreationCollisionOption.OpenIfExists);
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

        async Task InitializeEntries()
        {
            foreach (var file in await cacheFolder.GetFilesAsync())
            {
                string key = Path.GetFileNameWithoutExtension(file.Name);
                TimeSpan duration = GetDuration(file.FileType);
                entries.TryAdd(key, new CacheEntry() { Origin = file.DateCreated.UtcDateTime, TimeToLive = duration, FileName = file.Name });
            }
        }

        private TimeSpan GetDuration(string text)
        {
            string textToParse = text.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (textToParse == null)
                return defaultDuration;

            int duration;
            return Int32.TryParse(textToParse, out duration) ? TimeSpan.FromSeconds(duration) : defaultDuration;
        }

        async Task CleanCallback()
        {
            KeyValuePair<string, CacheEntry>[] kvps;
            var now = DateTime.UtcNow;
            kvps = entries.Where(kvp => kvp.Value.Origin + kvp.Value.TimeToLive < now).ToArray();

            System.Diagnostics.Debug.WriteLine(string.Format("DiskCacher: Removing {0} elements from the cache", kvps.Length));

            foreach (var kvp in kvps)
            {
                CacheEntry oldCacheEntry;
                if (entries.TryRemove(kvp.Key, out oldCacheEntry))
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
        }

        /// <summary>
        /// GetFilePath
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<string> GetFilePathAsync(string key)
        {
            await initTask.ConfigureAwait(false);

            var sanitizedKey = SanitizeKey(key);

            CacheEntry entry;
            if (!entries.TryGetValue(sanitizedKey, out entry))
                return null;

            return Path.Combine(cacheFolder.Path, entry.FileName);
        }

        /// <summary>
        /// Checks if cache entry exists/
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="key">Key.</param>
        public async Task<bool> ExistsAsync(string key)
        {
            key = SanitizeKey(key);

            await initTask.ConfigureAwait(false);

            return entries.ContainsKey(key);
        }

        /// <summary>
        /// Adds the file to cache and file saving queue if not exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="bytes">Bytes.</param>
        /// <param name="duration">Duration.</param>
        public async Task AddToSavingQueueIfNotExistsAsync(string key, byte[] bytes, TimeSpan duration)
        {
            await initTask.ConfigureAwait(false);

            var sanitizedKey = SanitizeKey(key);

            if (!fileWritePendingTasks.TryAdd(sanitizedKey, 1))
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

                        string filename = sanitizedKey + "." + duration.TotalSeconds;

                        var file = await cacheFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                        using (var fs = await file.OpenStreamForWriteAsync().ConfigureAwait(false))
                        {
                            await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                        }

                        entries[sanitizedKey] = new CacheEntry(DateTime.UtcNow, duration, filename);
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
                        fileWritePendingTasks.TryRemove(sanitizedKey, out finishedTask);
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
        /// Tries to get cached file as byte array.
        /// </summary>
        /// <returns>The get async.</returns>
        /// <param name="key">Key.</param>
        /// <param name="token">Token.</param>
        public async Task<byte[]> TryGetAsync(string key, CancellationToken token)
        {
            await initTask.ConfigureAwait(false);

            key = SanitizeKey(key);
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
                catch (Exception)
                {
                    return null;
                }

                if (file == null)
                    return null;

                var buffer = await FileIO.ReadBufferAsync(file);
                return buffer.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tries to get cached file as stream.
        /// </summary>
        /// <returns>The get async.</returns>
        /// <param name="key">Key.</param>
        public async Task<Stream> TryGetStreamAsync(string key)
        {
            await initTask.ConfigureAwait(false);

            key = SanitizeKey(key);
            await WaitForPendingWriteIfExists(key).ConfigureAwait(false);

            try
            {
                CacheEntry entry;
                if (!entries.TryGetValue(key, out entry))
                    return null;

                var file = await cacheFolder.GetFileAsync(entry.FileName);

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
        public async Task RemoveAsync(string key)
        {
            await initTask.ConfigureAwait(false);

            key = SanitizeKey(key);

            await WaitForPendingWriteIfExists(key).ConfigureAwait(false);

            key = SanitizeKey(key);
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
        public async Task ClearAsync()
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
                    await item.DeleteAsync();
                }

                entries.Clear();
            }
            finally
            {
                fileWriteLock.Release();
            }
        }

        async Task WaitForPendingWriteIfExists(string key)
        {
            while (fileWritePendingTasks.ContainsKey(key))
            {
                await Task.Delay(20).ConfigureAwait(false);
            }
        }

        string SanitizeKey(string key)
        {
            return new string(key
                .Where(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                .ToArray());
        }
    }
}
