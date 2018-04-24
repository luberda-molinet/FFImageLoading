using Android.Graphics;
using Android.Graphics.Drawables;
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
        const int BYTES_PER_ARGB_8888_PIXEL = 4;
        const int LOW_MEMORY_BYTE_ARRAY_POOL_DIVISOR = 2;
        const int BITMAP_POOL_TARGET_SCREENS = 4;
        const int MEMORY_CACHE_TARGET_SCREENS = 4;
        const int ARRAY_POOL_SIZE_BYTES = 4 * 1024 * 1024;

        readonly ReuseBitmapDrawableCache<TValue> _cache;
        readonly ConcurrentDictionary<string, ImageInformation> _imageInformations;
        readonly IMiniLogger _logger;
        readonly object _lock = new object();

        public ImageCache(int maxCacheSize, IMiniLogger logger, bool verboseLogging)
        {
            _logger = logger;
            int memoryCacheSize;
            int bitmapPoolSize;

            var context = new ContextWrapper(Application.Context);
            var am = (ActivityManager)context?.GetSystemService(Context.ActivityService);

            bool amIsLowRamDevice = false;
            int amLargeMemoryClass = 128;
            int amMemoryClass = 48;

            if (am != null)
            {
                amIsLowRamDevice = am.IsLowRamDevice;
                amMemoryClass = am.MemoryClass;
                amLargeMemoryClass = am.LargeMemoryClass;
            }

            bool isLowMemoryDevice = Utils.HasKitKat() && amIsLowRamDevice;
            bool isLargeHeapEnabled = Utils.HasHoneycomb() && (context.ApplicationInfo.Flags & ApplicationInfoFlags.LargeHeap) != 0;
            int memoryClass = isLargeHeapEnabled ? amLargeMemoryClass : amMemoryClass;
            int maxSize = (int)(1024 * 1024 * (isLowMemoryDevice ? 0.33f * memoryClass : 0.4f * memoryClass));

            var metrics = context.Resources.DisplayMetrics;
            int widthPixels = metrics.WidthPixels;
            int heightPixels = metrics.HeightPixels;
            int screenSize = widthPixels * heightPixels * BYTES_PER_ARGB_8888_PIXEL;

            int targetBitmapPoolSize = screenSize * BITMAP_POOL_TARGET_SCREENS;
            int targetMemoryCacheSize = screenSize * MEMORY_CACHE_TARGET_SCREENS;
            int arrayPoolSize = isLowMemoryDevice ? ARRAY_POOL_SIZE_BYTES / LOW_MEMORY_BYTE_ARRAY_POOL_DIVISOR : ARRAY_POOL_SIZE_BYTES;
            int availableSize = maxSize - arrayPoolSize;

            if (maxCacheSize >= (int)(1024 * 1024 * 16))
            {
                float part = maxCacheSize / (BITMAP_POOL_TARGET_SCREENS + MEMORY_CACHE_TARGET_SCREENS);
                memoryCacheSize = (int)Math.Round(part * MEMORY_CACHE_TARGET_SCREENS);
                bitmapPoolSize = (int)Math.Round(part * BITMAP_POOL_TARGET_SCREENS);
            }
            else if (targetMemoryCacheSize + targetBitmapPoolSize <= availableSize)
            {
                memoryCacheSize = targetMemoryCacheSize;
                bitmapPoolSize = targetBitmapPoolSize;
            }
            else
            {
                float part = availableSize / (BITMAP_POOL_TARGET_SCREENS + MEMORY_CACHE_TARGET_SCREENS);
                memoryCacheSize = (int)Math.Round(part * MEMORY_CACHE_TARGET_SCREENS);
                bitmapPoolSize = (int)Math.Round(part * BITMAP_POOL_TARGET_SCREENS);
            }

            double sizeInMB = Math.Round(memoryCacheSize / 1024d / 1024d, 2);
            double poolSizeInMB = Math.Round(bitmapPoolSize / 1024d / 1024d, 2);
            logger.Debug(string.Format("Image memory cache size: {0} MB, reuse pool size:D {1} MB", sizeInMB, poolSizeInMB));

            _cache = new ReuseBitmapDrawableCache<TValue>(logger, memoryCacheSize, bitmapPoolSize, verboseLogging);
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
            lock (_lock)
            {
                ImageInformation imageInformation;

                if (_imageInformations.TryGetValue(key, out imageInformation))
                {
                    return imageInformation;
                }

                return null;
            }
        }

        public Tuple<TValue, ImageInformation> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            TValue drawable = null;

            lock (_lock)
            {
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
                else if (_imageInformations.ContainsKey(key))
                {
                    Remove(key, false);
                }
            }

            return null;
        }

        public void Add(string key, ImageInformation imageInformation, TValue bitmap)
        {
            if (string.IsNullOrWhiteSpace(key) || !bitmap.IsValidAndHasValidBitmap())
                return;

            lock (_lock)
            {

                if (_imageInformations.ContainsKey(key) || _cache.ContainsKey(key))
                    Remove(key, false);

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
    }
}
