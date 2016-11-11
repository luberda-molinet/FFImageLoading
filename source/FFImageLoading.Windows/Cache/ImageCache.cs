#if SILVERLIGHT
using FFImageLoading.Concurrency;
using System.Windows.Media.Imaging;
#else
using System.Collections.Concurrent;
using Windows.UI.Xaml.Media.Imaging;
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
        private readonly ConcurrentDictionary<string, Tuple<WeakReference<WriteableBitmap>, ImageInformation>> _reusableBitmaps;
		private readonly IMiniLogger _logger;

        private ImageCache(int maxCacheSize, IMiniLogger logger)
        {
			_logger = logger;
            _reusableBitmaps = new ConcurrentDictionary<string, Tuple<WeakReference<WriteableBitmap>, ImageInformation>>();
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

            var weakRef = new WeakReference<WriteableBitmap>(bitmap);
            _reusableBitmaps.TryAdd(key, new Tuple<WeakReference<WriteableBitmap>, ImageInformation>(weakRef, imageInformation));
        }

		public ImageInformation GetInfo(string key)
		{
			Tuple<WeakReference<WriteableBitmap>, ImageInformation> cacheEntry;
			if (_reusableBitmaps.TryGetValue (key, out cacheEntry))
			{
				return cacheEntry.Item2;
			}

			return null;
		}

        public Tuple<WriteableBitmap, ImageInformation> Get(string key)
        {
            CleanAbandonedItems();

            if (string.IsNullOrWhiteSpace(key))
                return null;

            Tuple<WeakReference<WriteableBitmap>, ImageInformation> cacheEntry;
            WriteableBitmap bitmap = null;

            if (_reusableBitmaps.TryGetValue(key, out cacheEntry) && cacheEntry.Item1.TryGetTarget(out bitmap))
            {
                return new Tuple<WriteableBitmap, ImageInformation>(bitmap, cacheEntry.Item2);
            }

            return null;
        }

        void CleanAbandonedItems()
        {
            foreach (var item in _reusableBitmaps)
            {
                WriteableBitmap bitmap = null;
                if (!item.Value.Item1.TryGetTarget(out bitmap) || bitmap == null)
                {
                    Tuple<WeakReference<WriteableBitmap>, ImageInformation> removed;
                    _reusableBitmaps.TryRemove(item.Key, out removed);
                }      
            }
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
			Tuple<WeakReference<WriteableBitmap>, ImageInformation> removed;
            _reusableBitmaps.TryRemove(key, out removed);
        }

		public void RemoveSimilar(string baseKey)
		{
            if (string.IsNullOrWhiteSpace(baseKey))
                return;
            
            var keysToRemove = _reusableBitmaps.Where(v => v.Value?.Item2?.BaseKey == baseKey).Select(v => v.Value?.Item2?.CacheKey).ToList();

			foreach (var key in keysToRemove)
			{
				Remove(key);
			}
		}
    }
}
