/*
 * The whole implementation comes from Jeremy Laval
 * See his Gist here: https://gist.githubusercontent.com/garuma/4323075/raw/3836b11fcd989141f97db7669becadd9835e43f0/DiskCache.cs
 * 
 * It is slightly modified:
 *  - to have async IO
 *  - to handle Blob rather than Bitmaps
 *  - to use conccurent dictionary
 */

using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using FFImageLoading.Cache;

#if SILVERLIGHT
using FFImageLoading.Concurrency;
#else
using System.Collections.Concurrent;
#endif

namespace FFImageLoading.Cache
{
    /// <summary>
    /// Disk cache Windows implementation.
    /// </summary>
    public class DiskCache : IDiskCache
    {
        private static readonly SemaphoreSlim journalLock = new SemaphoreSlim(initialCount: 1);
		private static readonly SemaphoreSlim fileWriteLock = new SemaphoreSlim(initialCount: 1);

        enum JournalOp
        {
            Created = 'c',
            Modified = 'm',
            Deleted = 'd'
        }

        const string JournalFileName = "FFImageLoading.journal";
        const string Magic = "MONOID";
		const int BufferSize = 4096; // Xamarin large object heap threshold is 8K
        readonly Encoding encoding = Encoding.UTF8;
        readonly Encoding encodingWrite = new UTF8Encoding(false);

        Task initTask = null;
        string version;
        string cacheFolderName;
        StorageFolder cacheFolder;
        StorageFile journalFile;
		ConcurrentDictionary<string, byte> fileWritePendingTasks; // we use it as an Hashset, since there's no ConcurrentHashset

        ConcurrentDictionary<string, CacheEntry> entries;

        public DiskCache(string cacheFolderName, string version)
        {
            this.entries = new ConcurrentDictionary<string, CacheEntry>();
            this.version = version;
            this.cacheFolder = null;
            this.journalFile = null;
            this.cacheFolderName = cacheFolderName;
            this.fileWritePendingTasks = new ConcurrentDictionary<string, byte>();

            initTask = Init();
        }

        /// <summary>
        /// Creates new cache default instance.
        /// </summary>
        /// <returns>The cache.</returns>
        /// <param name="cacheName">Cache name.</param>
        /// <param name="version">Version.</param>
        public static DiskCache CreateCache(string cacheName, string version = "1.0")
        {
            return new DiskCache(cacheName, version);
        }

        async Task Init()
        {
            try
            {
				cacheFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(cacheFolderName, CreationCollisionOption.OpenIfExists);
				await InitializeWithJournal().ConfigureAwait(false);
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

        async Task InitializeWithJournal()
        {
			await journalLock.WaitAsync().ConfigureAwait(false);

            try
            {
                try
                {
					journalFile = await cacheFolder.GetFileAsync(JournalFileName);
                }
                catch (Exception)
                {
                    journalFile = null;
                }
                
                if (journalFile == null)
                {
					journalFile = await cacheFolder.CreateFileAsync(JournalFileName, CreationCollisionOption.ReplaceExisting);

                    return;
                }
                else
                {
                    string line = null;

                    using (var stream = await journalFile.OpenStreamForReadAsync().ConfigureAwait(false))
                    using (var reader = new StreamReader(stream, encoding))
                    {
                        stream.Seek(0, SeekOrigin.Begin);

						while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                        {
                            try
                            {
                                var trimmedLine = line.Trim();

                                JournalOp op = ParseOp(trimmedLine);
                                string key;
                                DateTime origin;
                                TimeSpan duration;

                                switch (op)
                                {
                                    case JournalOp.Created:
                                        ParseEntry(trimmedLine, out key, out origin, out duration);
                                        entries.TryAdd(key, new CacheEntry(origin, duration, null));
                                        break;
                                    case JournalOp.Modified:
                                        ParseEntry(trimmedLine, out key, out origin, out duration);
                                        entries[key] = new CacheEntry(origin, duration, null);
                                        break;
                                    case JournalOp.Deleted:
                                        ParseEntry(trimmedLine, out key);
                                        CacheEntry oldEntry;
                                        entries.TryRemove(key, out oldEntry);
                                        break;
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            finally
            {
                journalLock.Release();
            }
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
						var file = await cacheFolder.GetFileAsync(kvp.Key);
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
            return Path.Combine(cacheFolder.Path, sanitizedKey);
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
        public async void AddToSavingQueueIfNotExists(string key, byte[] bytes, TimeSpan duration)
        {
            await initTask.ConfigureAwait(false);

            var sanitizedKey = SanitizeKey(key);

			if (fileWritePendingTasks.TryAdd(sanitizedKey, 1))
			{
#pragma warning disable 4014
				Task.Run(async () =>
				{
					await initTask.ConfigureAwait(false);

	            	try
	                {
						await fileWriteLock.WaitAsync().ConfigureAwait(false);

	                    bool existed = entries.ContainsKey(sanitizedKey);

	                    if (!existed)
	                    {
							var file = await cacheFolder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);

							using (var fs = await file.OpenStreamForWriteAsync().ConfigureAwait(false))
	                        {
	                            await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
	                        }
	                    }

						await AppendToJournalAsync(existed ? JournalOp.Modified : JournalOp.Created, sanitizedKey, DateTime.UtcNow, duration).ConfigureAwait(false);
	                    entries[sanitizedKey] = new CacheEntry(DateTime.UtcNow, duration, null);
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
#pragma warning restore 4014
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
            
            if (!entries.ContainsKey(key))
                return null;

            try
            {
                StorageFile file = null;

                try
                {
					file = await cacheFolder.GetFileAsync(key);
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

            if (!entries.ContainsKey(key))
                return null;

            try
            {
				var file = await cacheFolder.GetFileAsync(key);

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
			await journalLock.WaitAsync().ConfigureAwait(false);

            CacheEntry oldCacheEntry;
            if (entries.TryRemove(key, out oldCacheEntry))
            {
                try
                {
					var file = await cacheFolder.GetFileAsync(key);
					await file.DeleteAsync();
                }
                catch
                {
                }

				await AppendToJournalAsync(JournalOp.Deleted, key).ConfigureAwait(false);
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

			await journalLock.WaitAsync().ConfigureAwait(false);

            try
            {
                var entriesToRemove = entries.ToList();

                foreach (var kvp in entriesToRemove)
                {
                    CacheEntry oldCacheEntry;
                    if (entries.TryRemove(kvp.Key, out oldCacheEntry))
                    {
                        try
                        {
							var file = await cacheFolder.GetFileAsync(kvp.Key);
							await file.DeleteAsync();
                        }
                        catch
                        {
                        }
                    }
                }
                
				journalFile = await cacheFolder.CreateFileAsync(JournalFileName, CreationCollisionOption.ReplaceExisting);
            }
            finally
            {
                journalLock.Release();
            }
        }

        async Task WaitForPendingWriteIfExists(string key)
        {
            while (fileWritePendingTasks.ContainsKey(key))
            {
				await Task.Delay(20).ConfigureAwait(false);
            }
        }

        JournalOp ParseOp(string line)
        {
            return (JournalOp)line[0];
        }

        void ParseEntry(string line, out string key)
        {
            key = line.Substring(2);
        }

        void ParseEntry(string line, out string key, out DateTime origin, out TimeSpan duration)
        {
            key = null;
            origin = DateTime.MinValue;
            duration = TimeSpan.MinValue;

            var parts = line.Substring(2).Split(' ');
            if (parts.Length != 3)
                throw new InvalidOperationException("Invalid entry");
            key = parts[0];

            long dateTime, timespan;

            if (!long.TryParse(parts[1], out dateTime))
                throw new InvalidOperationException("Corrupted origin");
            else
                origin = new DateTime(dateTime);

            if (!long.TryParse(parts[2], out timespan))
                throw new InvalidOperationException("Corrupted duration");
            else
                duration = TimeSpan.FromMilliseconds(timespan);
        }

        async Task AppendToJournalAsync(JournalOp op, string key)
        {
			await journalLock.WaitAsync().ConfigureAwait(false);

            try
            {
				using (var stream = await journalFile.OpenStreamForWriteAsync().ConfigureAwait(false))
                using (var writer = new StreamWriter(stream, encodingWrite))
                {
                    stream.Seek(0, SeekOrigin.End);
					await writer.WriteAsync((char)op).ConfigureAwait(false);
					await writer.WriteAsync(' ').ConfigureAwait(false);
					await writer.WriteAsync(key).ConfigureAwait(false);
					await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                journalLock.Release();
            }
        }

        async Task AppendToJournalAsync(JournalOp op, string key, DateTime origin, TimeSpan ttl)
        {
			await journalLock.WaitAsync().ConfigureAwait(false);

            try
            {
				using (var stream = await journalFile.OpenStreamForWriteAsync().ConfigureAwait(false))
                using (var writer = new StreamWriter(stream, encodingWrite))
                {
                    stream.Seek(0, SeekOrigin.End);
                    await writer.WriteAsync((char)op).ConfigureAwait(false);
					await writer.WriteAsync(' ').ConfigureAwait(false);
					await writer.WriteAsync(key).ConfigureAwait(false);
					await writer.WriteAsync(' ').ConfigureAwait(false);
					await writer.WriteAsync(origin.Ticks.ToString()).ConfigureAwait(false);
					await writer.WriteAsync(' ').ConfigureAwait(false);
					await writer.WriteAsync(((long)ttl.TotalMilliseconds).ToString()).ConfigureAwait(false);
					await writer.WriteLineAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                journalLock.Release();
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
