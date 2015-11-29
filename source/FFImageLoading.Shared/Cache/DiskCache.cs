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
	public class DiskCache: IDiskCache
    {
        enum JournalOp {
            Created = 'c',
            Modified = 'm',
            Deleted = 'd'
        }

        const string JournalFileName = ".journal";
        const string Magic = "MONOID";
		const int BufferSize = 4096; // default value of .NET framework for CopyToAsync buffer size
        readonly Encoding encoding = Encoding.UTF8;
        string basePath;
        string journalPath;
        string version;
		object lockJournal;

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

        public DiskCache (string basePath, string version)
        {
            // Can't use minilogger here, we would have too many dependencies
            System.Diagnostics.Debug.WriteLine("DiskCache path: " + basePath);

            this.basePath = basePath;
            if (!Directory.Exists (basePath))
                Directory.CreateDirectory (basePath);
            this.journalPath = Path.Combine (basePath, JournalFileName);
            this.version = version;
			this.lockJournal = new object();

            try {
                InitializeWithJournal ();
            } catch {
                Directory.Delete (basePath, true);
                Directory.CreateDirectory (basePath);
            }

            ThreadPool.QueueUserWorkItem (CleanCallback);
        }

        public static DiskCache CreateCache (string cacheName, string version = "1.0")
        {
            string tmpPath = System.IO.Path.GetTempPath();
            string cachePath = Path.Combine(tmpPath, cacheName);

            return new DiskCache (cachePath, version);
        }

        void InitializeWithJournal ()
        {
			lock (lockJournal)
			{
				if (!File.Exists(journalPath))
				{
					using (var writer = new StreamWriter(journalPath, false, encoding))
					{
						writer.WriteLine(Magic);
						writer.WriteLine(version);
					}
					return;
				}

				string line = null;
				using (var reader = new StreamReader(journalPath, encoding))
				{
					if (!EnsureHeader(reader))
						throw new InvalidOperationException("Invalid header");
					while ((line = reader.ReadLine()) != null)
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

        void CleanCallback (object state)
        {
            KeyValuePair<string, CacheEntry>[] kvps;
            var now = DateTime.UtcNow;
            kvps = entries.Where (kvp => kvp.Value.Origin + kvp.Value.TimeToLive < now).Take (10).ToArray ();

            // Can't use minilogger here, we would have too many dependencies
            System.Diagnostics.Debug.WriteLine(string.Format("DiskCacher: Removing {0} elements from the cache", kvps.Length));

            foreach (var kvp in kvps)
            {
                CacheEntry oldCacheEntry;
                if (entries.TryRemove(kvp.Key, out oldCacheEntry)) {
                    try {
                        File.Delete(Path.Combine(basePath, kvp.Key));
                    } catch {
                    }
                }
            }
        }

        bool EnsureHeader (StreamReader reader)
        {
            var m = reader.ReadLine ();
            var v = reader.ReadLine ();

            return m == Magic && v == version;
        }

        JournalOp ParseOp (string line)
        {
            return (JournalOp)line[0];
        }

        void ParseEntry (string line, out string key)
        {
            key = line.Substring (2);
        }

        void ParseEntry (string line, out string key, out DateTime origin, out TimeSpan duration)
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

        public string BasePath
        {
            get
            {
                return basePath;
            }
        }

		public async Task AddOrUpdateAsync (string key, Stream stream, CancellationToken token, TimeSpan duration)
		{
			key = SanitizeKey (key);

			bool existed = entries.ContainsKey (key);
			string filepath = Path.Combine(basePath, key);

			using (var fs = FileStore.GetOutputStream(filepath))
			{
				await stream.CopyToAsync(fs, BufferSize, token).ConfigureAwait(false);
			}

			AppendToJournal (existed ? JournalOp.Modified : JournalOp.Created,
				key,
				DateTime.UtcNow,
				duration);
			entries[key] = new CacheEntry (DateTime.UtcNow, duration);
		}

		public void Remove(string key)
		{
			key = SanitizeKey (key);
			string filepath = Path.Combine(basePath, key);

			if (File.Exists(filepath))
				File.Delete(filepath);

			bool existed = entries.ContainsKey (key);
			if (existed)
				AppendToJournal(JournalOp.Deleted, key);
		}

		public void Clear()
		{
			lock (lockJournal)
			{
				Directory.Delete (basePath, true);
				Directory.CreateDirectory (basePath);
				entries.Clear();
			}
		}

        public async Task<byte[]> TryGetAsync (string key, CancellationToken token)
        {
            key = SanitizeKey (key);
            if (!entries.ContainsKey (key))
                return null;
			
            try
			{
                string filepath = Path.Combine (basePath, key);
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

		public Task<Stream> TryGetStream (string key)
		{
			return Task<Stream>.Run(() => {
				key = SanitizeKey(key);
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
			});
		}

        void AppendToJournal (JournalOp op, string key)
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

        void AppendToJournal (JournalOp op, string key, DateTime origin, TimeSpan ttl)
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

        string SanitizeKey (string key)
        {
            return new string (key
                .Where (c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                .ToArray ());
        }
    }
}
