#if SILVERLIGHT
using FFImageLoading.Concurrency;
using System.Windows.Media.Imaging;
#else
using Windows.UI.Xaml.Media.Imaging;
using Windows.Security.ExchangeActiveSyncProvisioning;
#endif

using System;
using FFImageLoading.Work;
using FFImageLoading.Helpers;
using System.Linq;

namespace FFImageLoading.Cache
{
    class ImageCache : IImageCache
    {
        private static IImageCache _instance;
        private readonly WriteableBitmapLRUCache _reusableBitmaps;
        private readonly IMiniLogger _logger;

        private ImageCache(int maxCacheSize, IMiniLogger logger)
        {
            _logger = logger;

            if (maxCacheSize == 0)
            {
                //TODO Does anyone know how we could get available app ram from WinRT API?
                EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();
                if (deviceInfo.OperatingSystem.ToLowerInvariant().Contains("phone"))
                    maxCacheSize = 1000000 * 64; //64MB
                else
                    maxCacheSize = 1000000 * 256; //256MB

                _logger?.Debug($"Memory cache size: {maxCacheSize} bytes");
            }

            _reusableBitmaps = new WriteableBitmapLRUCache(maxCacheSize);
        }

        public static IImageCache Instance
        {
            get
            {
                return _instance ?? (_instance = new ImageCache(ImageService.Instance.Config.MaxMemoryCacheSize, ImageService.Instance.Config.Logger));
            }
        }

        public void Add(string key, ImageInformation imageInformation, BitmapSource bitmap)
        {
            if (string.IsNullOrWhiteSpace(key) || bitmap == null)
                return;

            _reusableBitmaps.TryAdd(key, new Tuple<BitmapSource, ImageInformation>(bitmap, imageInformation));
        }

        public ImageInformation GetInfo(string key)
        {
            Tuple<BitmapSource, ImageInformation> cacheEntry;
            if (_reusableBitmaps.TryGetValue (key, out cacheEntry))
            {
                return cacheEntry.Item2;
            }

            return null;
        }

        public Tuple<BitmapSource, ImageInformation> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            Tuple<BitmapSource, ImageInformation> cacheEntry;

            if (_reusableBitmaps.TryGetValue(key, out cacheEntry) && cacheEntry.Item1 != null)
            {
                return new Tuple<BitmapSource, ImageInformation>(cacheEntry.Item1, cacheEntry.Item2);
            }

            return null;
        }

        public void Clear()
        {
            _reusableBitmaps.Clear();
            GC.Collect();
        }

        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _logger.Debug (string.Format ("Called remove from memory cache for '{0}'", key));
            _reusableBitmaps.Remove(key);
        }

        public void RemoveSimilar(string baseKey)
        {
            if (string.IsNullOrWhiteSpace(baseKey))
                return;

            var pattern = baseKey + ";";

            var keysToRemove = _reusableBitmaps.Keys.Where(i => i.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var key in keysToRemove)
            {
                Remove(key);
            }
        }
    }
}
