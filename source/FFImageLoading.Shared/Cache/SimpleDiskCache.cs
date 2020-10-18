using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using FFImageLoading.IO;
using FFImageLoading.Config;
using FFImageLoading.Helpers;

namespace FFImageLoading.Cache
{
    /// <summary>
    /// Disk cache iOS/Android implementation.
    /// </summary>
    public class SimpleDiskCache : IDiskCache
    {
        const int BufferSize = 4096; // Xamarin large object heap threshold is 8K
        string _cachePath;
        ConcurrentDictionary<string, byte> _fileWritePendingTasks = new ConcurrentDictionary<string, byte>();
        readonly SemaphoreSlim _currentWriteLock = new SemaphoreSlim(1, 1);
        Task _currentWrite = Task.FromResult<byte>(1);
        ConcurrentDictionary<string, CacheEntry> _entries = new ConcurrentDictionary<string, CacheEntry>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FFImageLoading.Cache.SimpleDiskCache"/> class.
        /// </summary>
        /// <param name="cachePath">Cache path.</param>
        /// <param name="configuration">Configuration.</param>
        public SimpleDiskCache(string cachePath, Configuration configuration)
        {
            _cachePath = Path.GetFullPath(cachePath);
            Configuration = configuration;

            Logger?.Debug("SimpleDiskCache path: " + _cachePath);

            if (!Directory.Exists(_cachePath))
                Directory.CreateDirectory(_cachePath);

            InitializeEntries();

            ThreadPool.QueueUserWorkItem(CleanCallback);
        }

        protected Configuration Configuration { get; private set; }
        protected IMiniLogger Logger { get { return Configuration.Logger; } }

        /// <summary>
        /// Adds the file to cache and file saving queue if it does not exists.
        /// </summary>
        /// <param name="key">Key to store/retrieve the file.</param>
        /// <param name="bytes">File data in bytes.</param>
        /// <param name="duration">Specifies how long an item should remain in the cache.</param>
        /// <param name="writeFinished">Write finished.</param>
        public virtual async Task AddToSavingQueueIfNotExistsAsync(string key, byte[] bytes, TimeSpan duration, string uri = null,
			Action<FileWriteInfo> writeFinishedAction = null)
        {
            if (!_fileWritePendingTasks.TryAdd(key, 1))
            {
                Logger?.Error("Can't save to disk as another write with the same key is pending: " + key);
                return;
            }

            await _currentWriteLock.WaitAsync().ConfigureAwait(false); // Make sure we don't add multiple continuations to the same task
            try
            {
                _currentWrite = _currentWrite.ContinueWith(async t =>
                {
                    await Task.Yield(); // forces it to be scheduled for later

                    try
                    {
                        if (!Directory.Exists(_cachePath))
                            Directory.CreateDirectory(_cachePath);

                        CacheEntry oldEntry;
                        if (_entries.TryGetValue(key, out oldEntry))
                        {
                            string oldFilepath = Path.Combine(_cachePath, oldEntry.FileName);
                            if (File.Exists(oldFilepath))
                                File.Delete(oldFilepath);
                        }

                        string filename = key + "." + (long)duration.TotalSeconds;
                        string filepath = Path.Combine(_cachePath, filename);

                        await FileStore.WriteBytesAsync(filepath, bytes, CancellationToken.None).ConfigureAwait(false);

                        _entries[key] = new CacheEntry(DateTime.UtcNow, duration, filename);
						writeFinishedAction?.Invoke(new FileWriteInfo(filepath, uri));

                        if (Configuration.VerboseLogging)
                            Logger?.Debug(string.Format("File {0} saved to disk cache for key {1}", filepath, key));
                    }
                    catch (Exception ex) // Since we don't observe the task (it's not awaited, we should catch all exceptions)
                    {
                        Logger?.Error(string.Format("An error occured while writing to disk cache for {0}", key), ex);
                    }
                    finally
                    {
                        byte finishedTask;
                        _fileWritePendingTasks.TryRemove(key, out finishedTask);
                    }
                });
            }
            finally
            {
                _currentWriteLock.Release();
            }
        }

        /// <summary>
        /// Removes the specified cache entry.
        /// </summary>
        /// <param name="key">Key.</param>
        public virtual async Task RemoveAsync(string key)
        {
            await WaitForPendingWriteIfExists(key).ConfigureAwait(false);
            CacheEntry entry;
            if (_entries.TryRemove(key, out entry))
            {
                string filepath = Path.Combine(_cachePath, entry.FileName);

                if (File.Exists(filepath))
                    File.Delete(filepath);
            }
        }

        /// <summary>
        /// Clears all cache entries.
        /// </summary>
        public virtual async Task ClearAsync()
        {
            while (_fileWritePendingTasks.Count != 0)
            {
                await Task.Delay(20).ConfigureAwait(false);
            }

            try
            {
                Directory.Delete(_cachePath, true);
            }
            catch (DirectoryNotFoundException)
            {
            }

            Directory.CreateDirectory (_cachePath);
            _entries.Clear();
        }

        /// <summary>
        /// Checks if cache entry exists/
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="key">Key.</param>
        public virtual Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(_entries.ContainsKey(key));
        }

        /// <summary>
        /// Tries to get cached file as stream.
        /// </summary>
        /// <returns>The get stream.</returns>
        /// <param name="key">Key.</param>
        public virtual async Task<Stream> TryGetStreamAsync(string key)
        {
            await WaitForPendingWriteIfExists(key).ConfigureAwait(false);

            try
            {
                CacheEntry entry;
                if (!_entries.TryGetValue(key, out entry))
                    return null;

                string filepath = Path.Combine(_cachePath, entry.FileName);
                try
                {
                    return FileStore.GetInputStream(filepath, false);
                }
                catch (DirectoryNotFoundException)
                {
                    Directory.CreateDirectory(_cachePath);
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public virtual Task<string> GetFilePathAsync(string key)
        {
            CacheEntry entry;
            if (!_entries.TryGetValue(key, out entry))
                return Task.FromResult<string>(null);

            return Task.FromResult(Path.Combine(_cachePath, entry.FileName));
        }

        protected async Task WaitForPendingWriteIfExists(string key)
        {
            while (_fileWritePendingTasks.ContainsKey(key))
            {
                await Task.Delay(20).ConfigureAwait(false);
            }
        }

        protected void InitializeEntries()
        {
            foreach (var fileInfo in new DirectoryInfo(_cachePath).EnumerateFiles())
            {
                string key = Path.GetFileNameWithoutExtension(fileInfo.Name);
                var duration = GetDuration(fileInfo.Extension);
                _entries.TryAdd(key, new CacheEntry(fileInfo.CreationTimeUtc, duration, fileInfo.Name));
            }
        }

        protected TimeSpan GetDuration(string text)
        {
            string textToParse = text.Split(new[] { '.'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(textToParse))
                return Configuration.DiskCacheDuration;

            int duration;
            return int.TryParse(textToParse, out duration) ? TimeSpan.FromSeconds(duration) : Configuration.DiskCacheDuration;
        }

        protected virtual void CleanCallback(object state)
        {
            KeyValuePair<string, CacheEntry>[] kvps;
            var now = DateTime.UtcNow;
            kvps = _entries.Where(kvp => kvp.Value.Origin + kvp.Value.TimeToLive < now).ToArray();

            foreach (var kvp in kvps)
            {
                CacheEntry oldCacheEntry;
                if (_entries.TryRemove(kvp.Key, out oldCacheEntry))
                {
                    try
                    {
                        Logger.Debug(string.Format("SimpleDiskCache: Removing expired file {0}", oldCacheEntry.FileName));
                        File.Delete(Path.Combine(_cachePath, oldCacheEntry.FileName));
                    }
                    // Analysis disable once EmptyGeneralCatchClause
                    catch
                    {
                    }
                }
            }
        }
    }
}
