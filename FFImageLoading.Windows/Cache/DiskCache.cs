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

using Windows.Storage;

using System.Collections.Concurrent;
using System.IO;

namespace FFImageLoading.Cache
{
    public class DiskCache : IDiskCache
    {
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(initialCount: 1);

        enum JournalOp
        {
            Created = 'c',
            Modified = 'm',
            Deleted = 'd'
        }

        const string JournalFileName = "FFImageLoadingCache.journal";
        const string Magic = "MONOID";
        const int BufferSize = 4096; // default value of .NET framework for CopyToAsync buffer size
        readonly Encoding encoding = Encoding.UTF8;

        string version;
        string cacheFolderName;
        StorageFolder cacheFolder = null;
        StorageFile journalFile = null;

        Task isInitialized;

        struct CacheEntry
        {
            public DateTime Origin;
            public TimeSpan TimeToLive;

            public CacheEntry(DateTime o, TimeSpan ttl)
            {
                Origin = o;
                TimeToLive = ttl;
            }
        }

        ConcurrentDictionary<string, CacheEntry> entries;

        public DiskCache(string cacheFolderName, string version)
        {
            this.entries = new ConcurrentDictionary<string, CacheEntry>();
            this.version = version;
            this.cacheFolderName = cacheFolderName;
            this.isInitialized = Init();
        }

        public static DiskCache CreateCache(string cacheName, string version = "1.0")
        {
            return new DiskCache(cacheName, version);
        }

        async Task Init()
        {
            if (isInitialized != null)
                return;

            cacheFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(cacheFolderName, CreationCollisionOption.OpenIfExists);

            try
            {
                isInitialized = InitializeWithJournal();
                await isInitialized;
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

            await CleanCallback();
        }

        async Task InitializeWithJournal()
        {
            await semaphoreSlim.WaitAsync();

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

                    
                    //using (var stream = await journalFile.OpenStreamForWriteAsync())
                    //using (var writer = new StreamWriter(stream, encoding))
                    //{
                    //    await writer.WriteLineAsync(Magic);
                    //    await writer.WriteLineAsync(version);
                    //    await writer.FlushAsync();
                    //}

                    return;
                }
                else
                {
                    string line = null;

                    using (var stream = await journalFile.OpenStreamForReadAsync())
                    using (var reader = new StreamReader(stream, encoding))
                    {
                        /*
                        if (!EnsureHeader(reader))
                        {
                            throw new InvalidOperationException("Invalid header");
                        }
                        */
                            
                        while ((line = await reader.ReadLineAsync()) != null)
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
            finally
            {
                semaphoreSlim.Release();
            }
        }

        async Task CleanCallback()
        {
            await isInitialized;

            KeyValuePair<string, CacheEntry>[] kvps;
            var now = DateTime.UtcNow;
            kvps = entries.Where(kvp => kvp.Value.Origin + kvp.Value.TimeToLive < now).Take(10).ToArray();

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

        bool EnsureHeader(StreamReader reader)
        {
            var m = reader.ReadLine();
            var v = reader.ReadLine();

            System.Diagnostics.Debug.WriteLine("{0} / {1} ||| {2} / {3}", m, Magic, v, version);

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

        public string BasePath
        {
            get
            {
                return cacheFolder == null ? null : cacheFolder.Path;
            }
        }

        public async Task AddOrUpdateAsync(string key, Stream stream, CancellationToken token, TimeSpan duration)
        {
            await isInitialized;

            key = SanitizeKey(key);

            bool existed = entries.ContainsKey(key);

            var file = await cacheFolder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);

            using (var fs = await file.OpenStreamForWriteAsync())
            {
                await stream.CopyToAsync(fs, BufferSize, token).ConfigureAwait(false);
            }

            await AppendToJournal(existed ? JournalOp.Modified : JournalOp.Created,
                key,
                DateTime.UtcNow,
                duration);
            entries[key] = new CacheEntry(DateTime.UtcNow, duration);
        }

        public async Task<byte[]> TryGetAsync(string key, CancellationToken token)
        {
            await isInitialized;

            key = SanitizeKey(key);
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

                using (var fs = await file.OpenStreamForReadAsync())
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

        public async Task<Stream> TryGetStream(string key)
        {
            await isInitialized;

            key = SanitizeKey(key);
            if (!entries.ContainsKey(key))
                return null;

            try
            {
                var file = await cacheFolder.GetFileAsync(key);

                if (file == null)
                    return null;

                return await file.OpenStreamForReadAsync();
            }
            catch
            {
                return null;
            }
        }

        async Task AppendToJournal(JournalOp op, string key)
        {
            await isInitialized;
            await semaphoreSlim.WaitAsync();

            try
            {
                using (var stream = await journalFile.OpenStreamForWriteAsync())
                using (var writer = new StreamWriter(stream, encoding))
                {
                    await writer.WriteAsync((char)op);
                    await writer.WriteAsync(' ');
                    await writer.WriteAsync(key);
                    await writer.WriteLineAsync();
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        async Task AppendToJournal(JournalOp op, string key, DateTime origin, TimeSpan ttl)
        {
            await isInitialized;
            await semaphoreSlim.WaitAsync();

            try
            {
                using (var stream = await journalFile.OpenStreamForWriteAsync())
                using (var writer = new StreamWriter(stream, encoding))
                {
                    await writer.WriteAsync((char)op);
                    await writer.WriteAsync(' ');
                    await writer.WriteAsync(key);
                    await writer.WriteAsync(' ');
                    await writer.WriteAsync(origin.Ticks.ToString());
                    await writer.WriteAsync(' ');
                    await writer.WriteAsync(((long)ttl.TotalMilliseconds).ToString());
                    await writer.WriteLineAsync();
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        string SanitizeKey(string key)
        {
            return new string(key
                .Where(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                .ToArray());
        }

        public async void Remove(string key)
        {
            await isInitialized;
            await semaphoreSlim.WaitAsync();

            try
            {
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
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async void Clear()
        {
            await isInitialized;
            await semaphoreSlim.WaitAsync();

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
                semaphoreSlim.Release();
            }
        }
    }
}
