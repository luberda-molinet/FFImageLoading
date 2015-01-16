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

using Env = Android.OS.Environment;
using HGR.Mobile.Droid.ImageLoading.IO;
using System.Collections.Concurrent;

namespace HGR.Mobile.Droid.ImageLoading.Cache
{
    public class DiskCache
    {
        enum JournalOp {
            Created = 'c',
            Modified = 'm',
            Deleted = 'd'
        }

        const string JournalFileName = ".journal";
        const string Magic = "MONOID";
        readonly Encoding encoding = Encoding.UTF8;
        string basePath;
        string journalPath;
        string version;

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
            this.basePath = basePath;
            if (!Directory.Exists (basePath))
                Directory.CreateDirectory (basePath);
            this.journalPath = Path.Combine (basePath, JournalFileName);
            this.version = version;

            try {
                InitializeWithJournal ();
            } catch {
                Directory.Delete (basePath, true);
                Directory.CreateDirectory (basePath);
            }

            ThreadPool.QueueUserWorkItem (CleanCallback);
        }

        public static DiskCache CreateCache (Android.Content.Context ctx, string cacheName, string version = "1.0")
        {
            /*string cachePath = Env.ExternalStorageState == Env.MediaMounted
                || !Env.IsExternalStorageRemovable ? ctx.ExternalCacheDir.AbsolutePath : ctx.CacheDir.AbsolutePath;*/
            string cachePath = ctx.CacheDir.AbsolutePath;

            return new DiskCache (Path.Combine (cachePath, cacheName), version);
        }

        void InitializeWithJournal ()
        {
            if (!File.Exists (journalPath)) {
                using (var writer = new StreamWriter (journalPath, false, encoding)) {
                    writer.WriteLine (Magic);
                    writer.WriteLine (version);
                }
                return;
            }

            string line = null;
            using (var reader = new StreamReader (journalPath, encoding)) {
                if (!EnsureHeader (reader))
                    throw new InvalidOperationException ("Invalid header");
                while ((line = reader.ReadLine ()) != null) {
                    try {
                        var op = ParseOp (line);
                        string key;
                        DateTime origin;
                        TimeSpan duration;

                        switch (op) {
                        case JournalOp.Created:
                            ParseEntry (line, out key, out origin, out duration);
                            entries.TryAdd (key, new CacheEntry (origin, duration));
                            break;
                        case JournalOp.Modified:
                            ParseEntry (line, out key, out origin, out duration);
                            entries[key] = new CacheEntry (origin, duration);
                            break;
                        case JournalOp.Deleted:
                            ParseEntry (line, out key);
                            CacheEntry oldEntry;
                            entries.TryRemove (key, out oldEntry);
                            break;
                        }
                    } catch {
                        break;
                    }
                }
            }
        }

        void CleanCallback (object state)
        {
            KeyValuePair<string, CacheEntry>[] kvps;
            var now = DateTime.UtcNow;
            kvps = entries.Where (kvp => kvp.Value.Origin + kvp.Value.TimeToLive < now).Take (10).ToArray ();
            Android.Util.Log.Info ("DiskCacher", "Removing {0} elements from the cache", kvps.Length);
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

        public async Task AddOrUpdate (string key, byte[] data, TimeSpan duration)
        {
            key = SanitizeKey (key);

            bool existed = entries.ContainsKey (key);
            string filepath = Path.Combine(basePath, key);
            await FileStore.WriteBytes(filepath, data).ConfigureAwait(false);

            AppendToJournal (existed ? JournalOp.Modified : JournalOp.Created,
                key,
                DateTime.UtcNow,
                duration);
            entries[key] = new CacheEntry (DateTime.UtcNow, duration);
        }

        public async Task<byte[]> TryGet (string key)
        {
            key = SanitizeKey (key);
            byte[] data = null;
            if (!entries.ContainsKey (key))
                return null;
            try {
                string filepath = Path.Combine (basePath, key);
                data = await FileStore.ReadBytes(filepath).ConfigureAwait(false);
            } catch {
                return null;
            }

            return data;
        }

        void AppendToJournal (JournalOp op, string key)
        {
            using (var writer = new StreamWriter (journalPath, true, encoding)) {
                writer.Write ((char)op);
                writer.Write (' ');
                writer.Write (key);
                writer.WriteLine ();
            }
        }

        void AppendToJournal (JournalOp op, string key, DateTime origin, TimeSpan ttl)
        {
            using (var writer = new StreamWriter (journalPath, true, encoding)) {
                writer.Write ((char)op);
                writer.Write (' ');
                writer.Write (key);
                writer.Write (' ');
                writer.Write (origin.Ticks);
                writer.Write (' ');
                writer.Write ((long)ttl.TotalMilliseconds);
                writer.WriteLine ();
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
