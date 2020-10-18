using System;
using Foundation;
using FFImageLoading.Extensions;
using System.Collections.Concurrent;
using FFImageLoading.Work;
using FFImageLoading.Helpers;
using System.Linq;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif

namespace FFImageLoading.Cache
{
    public class ImageCache : IMemoryCache<PImage>
    {
        private readonly NSCache _cache;
        private static IMemoryCache<PImage> _instance;
        private readonly ConcurrentDictionary<string, ImageInformation> _imageInformations;
        private readonly IMiniLogger _logger;
        private readonly object _lock = new object();

        public ImageCache(int maxCacheSize, IMiniLogger logger)
        {
            _logger = logger;
            _cache = new NSCache();
            _imageInformations = new ConcurrentDictionary<string, ImageInformation>();

            if (maxCacheSize <= 0)
                _cache.TotalCostLimit = (nuint)(NSProcessInfo.ProcessInfo.PhysicalMemory * 0.2d); // 20% of physical memory
            else
                _cache.TotalCostLimit = (nuint)Math.Min((NSProcessInfo.ProcessInfo.PhysicalMemory * 0.05d), maxCacheSize);

            var sizeInMB = Math.Round(_cache.TotalCostLimit / 1024d / 1024d, 2);
            logger.Debug(string.Format("Image memory cache size: {0} MB", sizeInMB));

            // if we get a memory warning notification we should clear the cache
            NSNotificationCenter.DefaultCenter.AddObserver(new NSString("UIApplicationDidReceiveMemoryWarningNotification"), notif => Clear());
        }

        public static IMemoryCache<PImage> Instance => _instance ?? (_instance = new ImageCache(ImageService.Instance.Config.MaxMemoryCacheSize, ImageService.Instance.Config.Logger));

        public ImageInformation GetInfo(string key)
        {
            lock (_lock)
            {
                if (_imageInformations.TryGetValue(key, out var imageInformation))
                {
                    return imageInformation;
                }
            }

            return null;
        }

        public Tuple<PImage, ImageInformation> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            lock (_lock)
            {
                var image = (PImage)_cache.ObjectForKey(new NSString(key));
                if (image == null || image.Handle == IntPtr.Zero)
                {
                    Remove(key, false);
                    return null;
                }

                var imageInformation = GetInfo(key);
                return new Tuple<PImage, ImageInformation>(image, imageInformation);
            }
        }

        public void Add(string key, ImageInformation imageInformation, PImage value)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null || value.Handle == IntPtr.Zero)
                return;

            lock (_lock)
            {
                if (_imageInformations.ContainsKey(key))
                    Remove(key, false);

                _imageInformations.TryAdd(key, imageInformation);
                _cache.SetCost(value, new NSString(key), value.GetMemorySize());
            }
        }

        public void Remove(string key)
        {
            Remove(key, true);
        }

        private void Remove(string key, bool log)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (log && ImageService.Instance.Config.VerboseMemoryCacheLogging)
                _logger.Debug(string.Format($"Remove from memory cache called for {key}"));

            lock (_lock)
            {
                _cache.RemoveObjectForKey(new NSString(key));
                _imageInformations.TryRemove(key, out var imageInformation);
            }
        }

        public void RemoveSimilar(string baseKey)
        {
            if (string.IsNullOrWhiteSpace(baseKey))
                return;

            var pattern = baseKey + ";";

            var keysToRemove = _imageInformations.Keys.Where(i => i.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase)).ToList();
            foreach (var key in keysToRemove)
            {
                Remove(key);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cache.RemoveAllObjects();
                _imageInformations.Clear();
            }

            GC.Collect();
        }
    }
}

