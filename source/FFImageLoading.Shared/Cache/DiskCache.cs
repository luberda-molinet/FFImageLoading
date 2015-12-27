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
using System.IO;
using System.Text;
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
	public class DiskCache: IDiskCache
    {
        enum JournalOp 
		{
            Created = 'c',
            Modified = 'm',
            Deleted = 'd'
        }

        const string JournalFileName = ".journal";
        const string Magic = "MONOID";
		const int BufferSize = 4096; // Xamarin large object heap threshold is 8K
        readonly Encoding encoding = Encoding.UTF8;
        string basePath;
        string journalPath;
        string version;
		object lockJournal;
		readonly SemaphoreSlim fileWriteLock;
		ConcurrentDictionary<string, byte> fileWritePendingTasks; // we use it as an Hashset, since there's no ConcurrentHashset

        struct CacheEntry
        {
            public DateTime Origin;
            public TimeSpan TimeToLive;

            public CacheEntry (DateTime o, TimeSpan ttl)
            {
                Origin = o;
                TimeToLive = ttl;
            }
        }

        ConcurrentDictionary<string, CacheEntry> entries = new ConcurrentDictionary<string, CacheEntry> ();

		/// <summary>
		/// Initializes a new instance of the <see cref="FFImageLoading.Cache.DiskCache"/> class.
		/// </summary>
		/// <param name="basePath">Base path.</param>
		/// <param name="version">Version.</param>
        public DiskCache(string basePath, string version)
        {
            // Can't use minilogger here, we would have too many dependencies
            System.Diagnostics.Debug.WriteLine("DiskCache path: " + basePath);

            this.basePath = basePath;

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);
			
            this.journalPath = Path.Combine(basePath, JournalFileName);
            this.version = version;
			this.lockJournal = new object();
			this.fileWriteLock = new SemaphoreSlim(initialCount: 1);
			this.fileWritePendingTasks = new ConcurrentDictionary<string, byte>();

            try 
			{
                InitializeWithJournal ();
            } 
			catch 
			{
                Directory.Delete (basePath, true);
                Directory.CreateDirectory (basePath);
            }

            ThreadPool.QueueUserWorkItem(CleanCallback);
        }

		/// <summary>
		/// Creates new cache default instance.
		/// </summary>
		/// <returns>The cache.</returns>
		/// <param name="cacheName">Cache name.</param>
		/// <param name="version">Version.</param>
        public static DiskCache CreateCache(string cacheName, string version = "1.0")
        {
            string tmpPath = Path.GetTempPath();
            string cachePath = Path.Combine(tmpPath, cacheName);

            return new DiskCache(cachePath, version);
        }

		/// <summary>
		/// Gets the base path.
		/// </summary>
		/// <returns>The base path.</returns>
		public Task<string> GetBasePathAsync()
		{
			return Task.FromResult(basePath);
		}

		/// <summary>
		/// Adds the file to cache and file saving queue if not exists.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="bytes">Bytes.</param>
		/// <param name="duration">Duration.</param>
		public void AddToSavingQueueIfNotExists(string key, byte[] bytes, TimeSpan duration)
		{
			var sanitizedKey = SanitizeKey(key);

			if (fileWritePendingTasks.TryAdd(sanitizedKey, 1))
			{
				#pragma warning disable 4014
				Task.Run(async () =>
					{
						try
						{
							await fileWriteLock.WaitAsync().ConfigureAwait(false);

							bool existed = entries.ContainsKey(sanitizedKey);
							string filepath = Path.Combine(basePath, sanitizedKey);

							if (!existed)
							{
								using (var fs = FileStore.GetOutputStream(filepath))
								{
									await fs.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
								}						
							}

							AppendToJournal(existed ? JournalOp.Modified : JournalOp.Created, sanitizedKey, DateTime.UtcNow, duration);
							entries[sanitizedKey] = new CacheEntry(DateTime.UtcNow, duration);	
						}
						catch (Exception ex) // Since we don't observe the task (it's not awaited, we should catch all exceptions)
						{
							Console.WriteLine(string.Format("An error occured while caching to disk image '{0}'.", key));
							Console.WriteLine(ex.ToString());
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
		/// Removes the specified cache entry.
		/// </summary>
		/// <param name="key">Key.</param>
		public async Task RemoveAsync(string key)
		{
			key = SanitizeKey (key);

			await WaitForPendingWriteIfExists(key).ConfigureAwait(false);

			string filepath = Path.Combine(basePath, key);

			if (File.Exists(filepath))
				File.Delete(filepath);

			bool existed = entries.ContainsKey (key);
			if (existed)
				AppendToJournal(JournalOp.Deleted, key);
		}

		/// <summary>
		/// Clears all cache entries.
		/// </summary>
		public async Task ClearAsync()
		{
			while(fileWritePendingTasks.Count != 0)
			{
				await Task.Delay(20).ConfigureAwait(false);
			}

			lock (lockJournal)
			{
				Directory.Delete(basePath, true);
				Directory.CreateDirectory (basePath);
				entries.Clear();
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
			key = SanitizeKey (key);

			await WaitForPendingWriteIfExists(key).ConfigureAwait(false);

			if (!entries.ContainsKey(key))
				return null;

			try
			{
				string filepath = Path.Combine(basePath, key);
				if (!FileStore.Exists(filepath))
					return null;

				using (var fs = FileStore.GetInputStream(filepath))
				{
					using (var ms = new MemoryStream())
					{
						await fs.CopyToAsync(ms, BufferSize, token).ConfigureAwait(false);
						return ms.ToArray();
					}
				}
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
			key = SanitizeKey(key);

			await WaitForPendingWriteIfExists(key).ConfigureAwait(false);

			if (!entries.ContainsKey(key))
				return null;

			try
			{
				string filepath = Path.Combine(basePath, key);
				return FileStore.GetInputStream(filepath);
			}
			catch
			{
				return null;
			}	
		}

		async Task WaitForPendingWriteIfExists(string key)
		{
			while (fileWritePendingTasks.ContainsKey(key))
			{
				await Task.Delay(20).ConfigureAwait(false);
			}
		}

        void InitializeWithJournal()
        {
			lock (lockJournal)
			{
				if(!File.Exists(journalPath))
				{
					using(var writer = new StreamWriter(journalPath, false, encoding))
					{
						writer.WriteLine(Magic);
						writer.WriteLine(version);
					}
					return;
				}

				string line = null;
				using(var reader = new StreamReader(journalPath, encoding))
				{
					if (!EnsureHeader(reader))
						throw new InvalidOperationException("Invalid header");
					
					while((line = reader.ReadLine()) != null)
					{
						try
						{
							var op = ParseOp(line);
							string key;
							DateTime origin;
							TimeSpan duration;

							switch (op)
							{
								case JournalOp.Created:
									ParseEntry(line, out key, out origin, out duration);
									entries.TryAdd(key, new CacheEntry(origin, duration));
									break;
								case JournalOp.Modified:
									ParseEntry(line, out key, out origin, out duration);
									entries[key] = new CacheEntry(origin, duration);
									break;
								case JournalOp.Deleted:
									ParseEntry(line, out key);
									CacheEntry oldEntry;
									entries.TryRemove(key, out oldEntry);
									break;
							}
						}
						catch
						{
							break;
						}
					}
				}
			}
        }

        void CleanCallback(object state)
        {
            KeyValuePair<string, CacheEntry>[] kvps;
            var now = DateTime.UtcNow;
            kvps = entries.Where(kvp => kvp.Value.Origin + kvp.Value.TimeToLive < now).Take(10).ToArray();

            // Can't use minilogger here, we would have too many dependencies
            System.Diagnostics.Debug.WriteLine(string.Format("DiskCacher: Removing {0} elements from the cache", kvps.Length));

            foreach (var kvp in kvps)
            {
                CacheEntry oldCacheEntry;
                if (entries.TryRemove(kvp.Key, out oldCacheEntry)) 
				{
                    try 
					{
                        File.Delete(Path.Combine(basePath, kvp.Key));
                    } 
					catch 
					{
					}
                }
            }
        }

        bool EnsureHeader(StreamReader reader)
        {
            var m = reader.ReadLine();
            var v = reader.ReadLine();

            return m == Magic && v == version;
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

            var parts = line.Substring (2).Split (' ');

            if (parts.Length != 3)
                throw new InvalidOperationException ("Invalid entry");
			
            key = parts[0];

            long dateTime, timespan;

            if (!long.TryParse (parts[1], out dateTime))
                throw new InvalidOperationException ("Corrupted origin");
            else
                origin = new DateTime (dateTime);

            if (!long.TryParse (parts[2], out timespan))
                throw new InvalidOperationException ("Corrupted duration");
            else
                duration = TimeSpan.FromMilliseconds (timespan);
        }

        void AppendToJournal(JournalOp op, string key)
        {
			lock (lockJournal)
			{
				using (var writer = new StreamWriter(journalPath, true, encoding))
				{
					writer.Write((char)op);
					writer.Write(' ');
					writer.Write(key);
					writer.WriteLine();
				}
			}
        }

        void AppendToJournal(JournalOp op, string key, DateTime origin, TimeSpan ttl)
        {
			lock (lockJournal)
			{
				using (var writer = new StreamWriter(journalPath, true, encoding))
				{
					writer.Write((char)op);
					writer.Write(' ');
					writer.Write(key);
					writer.Write(' ');
					writer.Write(origin.Ticks);
					writer.Write(' ');
					writer.Write((long)ttl.TotalMilliseconds);
					writer.WriteLine();
				}
			}
        }

        string SanitizeKey(string key)
        {
            return new string (key
                .Where (c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                .ToArray ());
        }
    }
}
