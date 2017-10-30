using System;
using FFImageLoading.Cache;
using FFImageLoading.Work;
using System.Collections.Generic;
using System.Linq;

namespace FFImageLoading.Mock
{
    public class MockImageCache : IMemoryCache<MockBitmap>
    {
        readonly Dictionary<string, Tuple<MockBitmap, ImageInformation>> _cache = new Dictionary<string, Tuple<MockBitmap, ImageInformation>>();
        readonly object _lock = new object();

        public static MockImageCache Instance { get; private set; } = new MockImageCache();

        public void Add(string key, ImageInformation imageInformation, MockBitmap bitmap)
        {
            lock (_lock)
            {
                _cache[key] = new Tuple<MockBitmap, ImageInformation>(bitmap, imageInformation);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }

        public Tuple<MockBitmap, ImageInformation> Get(string key)
        {
            lock (_lock)
            {
                Tuple<MockBitmap, ImageInformation> result = null;
                _cache.TryGetValue(key, out result);
                return result;
            }
        }

        public ImageInformation GetInfo(string key)
        {
            lock (_lock)
            {
                return Get(key)?.Item2;
            }
        }

        public void Remove(string key)
        {
            lock (_lock)
            {
                _cache.Remove(key);
            }
        }

        public void RemoveSimilar(string baseKey)
        {
            lock (_lock)
            {
                var pattern = baseKey + ";";

                var keysToRemove = _cache.Keys.Where(i => i.StartsWith(pattern, StringComparison.CurrentCultureIgnoreCase)).ToList();
                foreach (var key in keysToRemove)
                {
                    Remove(key);
                }
            }
        }
    }
}
