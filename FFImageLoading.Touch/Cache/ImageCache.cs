using System;
using UIKit;
using Foundation;
using FFImageLoading.Extensions;

namespace FFImageLoading.Cache
{
    internal class ImageCache: IImageCache
    {
        private readonly NSCache _cache;
        private static IImageCache _instance;

        private ImageCache(int maxCacheSize)
        {
            _cache = new NSCache();
            _cache.TotalCostLimit = (nuint)(NSProcessInfo.ProcessInfo.PhysicalMemory * 0.2); // 20% of physical memory

            // Can't use minilogger here, we would have too many dependencies
            decimal sizeInMB = System.Math.Round((decimal)_cache.TotalCostLimit/(1024*1024), 2);
            System.Diagnostics.Debug.WriteLine(string.Format("LruCache size: {0}MB", sizeInMB));

            // if we get a memory warning notification we should clear the cache
            NSNotificationCenter.DefaultCenter.AddObserver(new NSString("UIApplicationDidReceiveMemoryWarningNotification"), notif => Clear());
        }

        public static IImageCache Instance
        {
            get
            {
                return _instance ?? (_instance = new ImageCache(ImageService.Config.MaxCacheSize));
            }
        }

        public UIImage Get(string key)
        {
            return (UIImage)_cache.ObjectForKey(new NSString(key));
        }

        public void Add(string key, UIImage value)
        {
            _cache.SetCost(value, new NSString(key), value.GetMemorySize());
        }

		public void Clear()
		{
			_cache.RemoveAllObjects();
			System.GC.Collect(); // FMT: it's probably better to collect the GC since we need memory back?
		}
    }
}

