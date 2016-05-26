using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using FFImageLoading.IO;
using FFImageLoading.Cache;

namespace FFImageLoading.Cache
{
	/// <summary>
	/// Disk cache iOS/Android implementation.
	/// </summary>
	public class SimpleDiskCache: IDiskCache
    {
		const int BufferSize = 4096; // Xamarin large object heap threshold is 8K
		private string _cachePath;
		private ConcurrentDictionary<string, byte> _fileWritePendingTasks; // we use it as an Hashset, since there's no ConcurrentHashset
        private readonly SemaphoreSlim _currentWriteLock;
        private Task _currentWrite;
		private ConcurrentDictionary<string, CacheEntry> _entries = new ConcurrentDictionary<string, CacheEntry> ();
		private readonly TimeSpan _defaultDuration;

		/// <summary>
		/// Initializes a new instance of the <see cref="FFImageLoading.Cache.SimpleDiskCache"/> class.
		/// </summary>
		/// <param name="cachePath">Cache path.</param>
		public SimpleDiskCache(string cachePath)
        {
            // Can't use minilogger here, we would have too many dependencies
            System.Diagnostics.Debug.WriteLine("SimpleDiskCache path: " + cachePath);

			_cachePath = cachePath;

            if (!Directory.Exists(cachePath))
                Directory.CreateDirectory(cachePath);

			_fileWritePendingTasks = new ConcurrentDictionary<string, byte>();
			_currentWrite = Task.FromResult<byte>(1);
            _currentWriteLock = new SemaphoreSlim(1);
			_defaultDuration = new TimeSpan(30, 0, 0, 0);  // the default is 30 days
				
			InitializeEntries();

            ThreadPool.QueueUserWorkItem(CleanCallback);
        }

		/// <summary>
		/// Creates new cache default instance.
		/// </summary>
		/// <returns>The cache.</returns>
		/// <param name="cacheName">Cache name.</param>
		public static SimpleDiskCache CreateCache(string cacheName)
        {
            string tmpPath = Path.GetTempPath();
            string cachePath = Path.Combine(tmpPath, cacheName);

			return new SimpleDiskCache(cachePath);
        }

		/// <summary>
		/// Adds the file to cache and file saving queue if it does not exists.
		/// </summary>
		/// <param name="key">Key to store/retrieve the file.</param>
		/// <param name="bytes">File data in bytes.</param>
		/// <param name="duration">Specifies how long an item should remain in the cache.</param>
		public async void AddToSavingQueueIfNotExists(string key, byte[] bytes, TimeSpan duration)
		{
			var sanitizedKey = SanitizeKey(key);

            if (!_fileWritePendingTasks.TryAdd(sanitizedKey, 1))
                return;

            await _currentWriteLock.WaitAsync().ConfigureAwait(false); // Make sure we don't add multiple continuations to the same task
            try
            {
                _currentWrite = _currentWrite.ContinueWith(async t =>
                {
                    await Task.Yield(); // forces it to be scheduled for later

                    try
                    {
                        CacheEntry oldEntry;
                        if (_entries.TryGetValue(sanitizedKey, out oldEntry))
                        {
                            string oldFilepath = Path.Combine(_cachePath, oldEntry.FileName);
                            if (File.Exists(oldFilepath))
                                File.Delete(oldFilepath);
                        }

                        string filename = sanitizedKey + "." + duration.TotalSeconds;
                        string filepath = Path.Combine(_cachePath, filename);

                        using (var fs = FileStore.GetOutputStream(filepath))
                        {
                            await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                        }

                        _entries[sanitizedKey] = new CacheEntry(DateTime.UtcNow, duration, filename);
                    }
                    catch (Exception ex) // Since we don't observe the task (it's not awaited, we should catch all exceptions)
                    {
                        Console.WriteLine(string.Format("An error occured while caching to disk image '{0}'.", key));
                        Console.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        byte finishedTask;
                        _fileWritePendingTasks.TryRemove(sanitizedKey, out finishedTask);
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
		public async Task RemoveAsync(string key)
		{
			var sanitizedKey = SanitizeKey(key);

			await WaitForPendingWriteIfExists(sanitizedKey).ConfigureAwait(false);
			CacheEntry entry;
			if (_entries.TryRemove(sanitizedKey, out entry))
			{
				string filepath = Path.Combine(_cachePath, entry.FileName);

				if (File.Exists(filepath))
					File.Delete(filepath);
			}
		}

		/// <summary>
		/// Clears all cache entries.
		/// </summary>
		public async Task ClearAsync()
		{
			while (_fileWritePendingTasks.Count != 0)
			{
				await Task.Delay(20).ConfigureAwait(false);
			}

			Directory.Delete(_cachePath, true);
			Directory.CreateDirectory (_cachePath);
			_entries.Clear();
		}

		/// <summary>
		/// Checks if cache entry exists/
		/// </summary>
		/// <returns>The async.</returns>
		/// <param name="key">Key.</param>
		public Task<bool> ExistsAsync(string key)
		{
			key = SanitizeKey(key);
			return Task.FromResult(_entries.ContainsKey(key));
		}

		/// <summary>
		/// Tries to get cached file as byte array.
		/// </summary>
		/// <returns>The get async.</returns>
		/// <param name="key">Key.</param>
		/// <param name="token">Token.</param>
		public async Task<byte[]> TryGetAsync(string key, CancellationToken token)
		{
			var sanitizedKey = SanitizeKey (key);
			await WaitForPendingWriteIfExists(sanitizedKey).ConfigureAwait(false);

			try
			{
				CacheEntry entry;
				if (!_entries.TryGetValue(sanitizedKey, out entry))
					return null;
					
				string filepath = Path.Combine(_cachePath, entry.FileName);
				if (!FileStore.Exists(filepath))
					return null;

				return await FileStore.ReadBytesAsync(filepath, token).ConfigureAwait(false);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Tries to get cached file as stream.
		/// </summary>
		/// <returns>The get stream.</returns>
		/// <param name="key">Key.</param>
		public async Task<Stream> TryGetStreamAsync(string key)
		{
			var sanitizedKey = SanitizeKey(key);
			await WaitForPendingWriteIfExists(sanitizedKey).ConfigureAwait(false);

			try
			{
				CacheEntry entry;
				if (!_entries.TryGetValue(sanitizedKey, out entry))
					return null;

				string filepath = Path.Combine(_cachePath, entry.FileName);
				return FileStore.GetInputStream(filepath);
			}
			catch
			{
				return null;
			}	
		}

		public Task<string> GetFilePathAsync(string key)
		{
			var sanitizedKey = SanitizeKey(key);

			CacheEntry entry;
			if (!_entries.TryGetValue(sanitizedKey, out entry))
				return Task.FromResult<string>(null);
			
			return Task.FromResult(Path.Combine(_cachePath, entry.FileName));
		}

		private async Task WaitForPendingWriteIfExists(string key)
		{
			while (_fileWritePendingTasks.ContainsKey(key))
			{
				await Task.Delay(20).ConfigureAwait(false);
			}
		}

		private void InitializeEntries()
		{
			foreach (FileInfo fileInfo in new DirectoryInfo(_cachePath).EnumerateFiles())
			{
				string key = Path.GetFileNameWithoutExtension(fileInfo.Name);
				TimeSpan duration = GetDuration(fileInfo.Extension);
				_entries.TryAdd(key, new CacheEntry() { Origin = fileInfo.CreationTimeUtc, TimeToLive = duration, FileName = fileInfo.Name });
			}
		}

		private TimeSpan GetDuration(string text)
		{
			string textToParse = text.Split(new[] { '.'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
			if (textToParse == null)
				return _defaultDuration;

			int duration;
			return Int32.TryParse(textToParse, out duration) ? TimeSpan.FromSeconds(duration) : _defaultDuration;
		}

		void CleanCallback(object state)
		{
			KeyValuePair<string, CacheEntry>[] kvps;
			var now = DateTime.UtcNow;
			kvps = _entries.Where(kvp => kvp.Value.Origin + kvp.Value.TimeToLive < now).ToArray();

			// Can't use minilogger here, we would have too many dependencies
			System.Diagnostics.Debug.WriteLine(string.Format("DiskCacher: Removing {0} elements from the cache", kvps.Length));

			foreach (var kvp in kvps)
			{
				CacheEntry oldCacheEntry;
				if (_entries.TryRemove(kvp.Key, out oldCacheEntry)) 
				{
					try 
					{
						File.Delete(Path.Combine(_cachePath, kvp.Key));
					} 
					// Analysis disable once EmptyGeneralCatchClause
					catch 
					{
					}
				}
			}
		}

        private string SanitizeKey(string key)
        {
            return new string (key
                .Where (c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                .ToArray ());
        }
    }
}
