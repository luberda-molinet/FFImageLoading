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
                if (deviceInfo.OperatingSystem.ToLower().Contains("phone"))
                    maxCacheSize = 1000000 * 32; //32MB
                else
                    maxCacheSize = 1000000 * 128; //128MB

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

        public void Add(string key, ImageInformation imageInformation, WriteableBitmap bitmap)
        {
            if (string.IsNullOrWhiteSpace(key) || bitmap == null)
                return;

            _reusableBitmaps.TryAdd(key, new Tuple<WriteableBitmap, ImageInformation>(bitmap, imageInformation));
        }

		public ImageInformation GetInfo(string key)
		{
			Tuple<WriteableBitmap, ImageInformation> cacheEntry;
			if (_reusableBitmaps.TryGetValue (key, out cacheEntry))
			{
				return cacheEntry.Item2;
			}

			return null;
		}

        public Tuple<WriteableBitmap, ImageInformation> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            Tuple<WriteableBitmap, ImageInformation> cacheEntry;

            if (_reusableBitmaps.TryGetValue(key, out cacheEntry) && cacheEntry.Item1 != null)
            {
                return new Tuple<WriteableBitmap, ImageInformation>(cacheEntry.Item1, cacheEntry.Item2);
            }

            return null;
        }

        public void Clear()
        {
            _reusableBitmaps.Clear();

            // Force immediate Garbage collection. Please note that is resource intensive.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForPendingFinalizers();
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
            
            var keysToRemove = _reusableBitmaps.Values.Where(v => v?.Item2?.BaseKey == baseKey).Select(v => v?.Item2?.CacheKey).ToList();

			foreach (var key in keysToRemove)
			{
				Remove(key);
			}
		}
    }
}
