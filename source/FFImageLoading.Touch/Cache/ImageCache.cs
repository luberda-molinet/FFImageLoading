using System;
using UIKit;
using Foundation;
using FFImageLoading.Extensions;
using System.Collections.Concurrent;
using FFImageLoading.Work;

namespace FFImageLoading.Cache
{
    internal class ImageCache: IImageCache
    {
        private readonly NSCache _cache;
        private static IImageCache _instance;
		private readonly ConcurrentDictionary<string, ImageInformation> _imageInformations;

        private ImageCache(int maxCacheSize)
        {
            _cache = new NSCache();
			_imageInformations = new ConcurrentDictionary<string, ImageInformation>();
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
                return _instance ?? (_instance = new ImageCache(ImageService.Instance.Config.MaxCacheSize));
            }
        }

		public Tuple<UIImage, ImageInformation> Get(string key)
        {
			ImageInformation imageInformation = null;
			_imageInformations.TryGetValue(key, out imageInformation);

			var image = (UIImage)_cache.ObjectForKey(new NSString(key));

			return new Tuple<UIImage, ImageInformation>(image, imageInformation);
        }

		public void Add(string key, ImageInformation imageInformation, UIImage value)
        {
			if (string.IsNullOrWhiteSpace(key) || value == null)
				return;

			_imageInformations.TryAdd(key, imageInformation);
            _cache.SetCost(value, new NSString(key), value.GetMemorySize());
        }

		public void Remove(string key)
		{
			_cache.RemoveObjectForKey(new NSString(key));
			ImageInformation imageInformation;
			_imageInformations.TryRemove(key, out imageInformation);
		}	

		public void Clear()
		{
			_cache.RemoveAllObjects();
			_imageInformations.Clear();
			// Force immediate Garbage collection. Please note that is resource intensive.
			System.GC.Collect();
			System.GC.WaitForPendingFinalizers ();
			System.GC.WaitForPendingFinalizers (); // Double call since GC doesn't always find resources to be collected: https://bugzilla.xamarin.com/show_bug.cgi?id=20503
			System.GC.Collect ();
		}
    }
}

