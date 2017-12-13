using Android.Graphics;
using Android.Graphics.Drawables;
using Exception = System.Exception;
using Math = System.Math;
using FFImageLoading.Helpers;
using Android.Content;
using Android.App;
using Android.Content.PM;
using FFImageLoading.Drawables;
using System;
using FFImageLoading.Work;
using System.Collections.Concurrent;
using System.Linq;

namespace FFImageLoading.Cache
{
    public class ImageCache : ImageCache<SelfDisposingBitmapDrawable>
    {
        private ImageCache(int maxCacheSize, IMiniLogger logger, bool verboseLogging) : base(maxCacheSize, logger, verboseLogging)
        {
        }

        static IImageCache<SelfDisposingBitmapDrawable> _instance;
        public static IImageCache<SelfDisposingBitmapDrawable> Instance
        {
            get
            {
                return _instance ?? (_instance = new ImageCache<SelfDisposingBitmapDrawable>(ImageService.Instance.Config.MaxMemoryCacheSize, ImageService.Instance.Config.Logger, ImageService.Instance.Config.VerboseMemoryCacheLogging));
            }
        }
    }

    public class ImageCache<TValue> : IImageCache<TValue> where TValue: Java.Lang.Object, ISelfDisposingBitmapDrawable
    {
        readonly ReuseBitmapDrawableCache<TValue> _cache;
        readonly ConcurrentDictionary<string, ImageInformation> _imageInformations;
        readonly IMiniLogger _logger;
        readonly object _lock = new object();

        public ImageCache(int maxCacheSize, IMiniLogger logger, bool verboseLogging)
        {
            _logger = logger;
            int safeMaxCacheSize = GetMaxCacheSize(maxCacheSize);

            double sizeInMB = Math.Round(safeMaxCacheSize / 1024d / 1024d, 2);
            logger.Debug(string.Format("Image memory cache size: {0} MB", sizeInMB));

            // consider low treshold as a third of maxCacheSize
            int lowTreshold = safeMaxCacheSize / 2;

            _cache = new ReuseBitmapDrawableCache<TValue>(logger, safeMaxCacheSize, lowTreshold, verboseLogging);
            _imageInformations = new ConcurrentDictionary<string, ImageInformation>();
        }

        public static int GetBitmapSize(BitmapDrawable bmp)
        {
            if (Utils.HasKitKat())
                return bmp.Bitmap.AllocationByteCount;

            if (Utils.HasHoneycombMr1())
                return bmp.Bitmap.ByteCount;

            return bmp.Bitmap.RowBytes*bmp.Bitmap.Height;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _imageInformations.Clear();
            }
        }

        public ImageInformation GetInfo(string key)
        {
            ImageInformation imageInformation;
            if (_imageInformations.TryGetValue(key, out imageInformation))
            {
                return imageInformation;
            }

            return null;
        }

        public Tuple<TValue, ImageInformation> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            TValue drawable = null;

            if (_cache.TryGetValue(key, out drawable))
            {
                if (!drawable.IsValidAndHasValidBitmap())
                {
                    Remove(key, false);
                    return null;
                }

                var imageInformation = GetInfo(key);
                return new Tuple<TValue, ImageInformation>(drawable, imageInformation);
            }

            return null;
        }

        public void Add(string key, ImageInformation imageInformation, TValue bitmap)
        {
            if (string.IsNullOrWhiteSpace(key) || !bitmap.IsValidAndHasValidBitmap())
                return;

            if (_imageInformations.ContainsKey(key) || _cache.ContainsKey(key))
                Remove(key, false);

            lock (_lock)
            {
                _imageInformations.TryAdd(key, imageInformation);
                _cache.Add(key, bitmap);
            }
        }

        public void Remove(string key)
        {
            Remove(key, true);
        }

        void Remove(string key, bool log)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (log && ImageService.Instance.Config.VerboseMemoryCacheLogging)
                _logger.Debug(string.Format($"Remove from memory cache called for {key}"));

            lock (_lock)
            {
                _cache.Remove(key);
                ImageInformation imageInformation;
                _imageInformations.TryRemove(key, out imageInformation);
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

        /// <summary>
        /// Attempts to find a bitmap suitable for reuse based on the given dimensions.
        /// Note that any returned instance will have SetIsRetained(true) called on it
        /// to ensure that it does not release its resources prematurely as it is leaving
        /// cache management. This means you must call SetIsRetained(false) when you no
        /// longer need the instance.
        /// </summary>
        /// <returns>A ISelfDisposingBitmapDrawable.</returns>
        /// <param name="options">Bitmap creation options.</param>
        public TValue GetBitmapDrawableFromReusableSet(BitmapFactory.Options options)
        {
            return _cache.GetReusableBitmapDrawable(options);
        }

        public void AddToReusableSet(TValue value)
        {
            _cache.AddToReusePool(value);
        }

        private static int GetMaxCacheSize(int maxCacheSize)
        {
            if (maxCacheSize <= 0)
                return GetCacheSizeInPercent(0.2f); // DEFAULT 20%

            return Math.Max(GetCacheSizeInPercent(0.05f), maxCacheSize); // MIN SAFE LIMIT 5%
        }

        /// <summary>
        /// Gets the memory cache size based on a percentage of the max available VM memory.
        /// </summary>
        /// <example>setting percent to 0.2 would set the memory cache to one fifth of the available memory</example>
        /// <param name="percent">Percent of available app memory to use to size memory cache</param>
        /// <returns></returns>
        private static int GetCacheSizeInPercent(float percent)
        {
            if (percent < 0.01f || percent > 0.8f)
                throw new Exception("GetCacheSizeInPercent - percent must be between 0.01 and 0.8 (inclusive)");

            var context = new Android.Content.ContextWrapper(Android.App.Application.Context);
            var am = (ActivityManager) context.GetSystemService(Context.ActivityService);
            bool largeHeap = (context.ApplicationInfo.Flags & ApplicationInfoFlags.LargeHeap) != 0;
            int memoryClass = am.MemoryClass;

            if (largeHeap && Utils.HasHoneycomb())
            {
                memoryClass = am.LargeMemoryClass;
            }

            int availableMemory = 1024 * 1024 * memoryClass;
            return (int)Math.Round(percent * availableMemory);
        }
    }
}
