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
using System.Collections.Generic;

namespace FFImageLoading.Cache
{
	public class ImageCache : IImageCache
	{
		private static IImageCache _instance;
		private readonly ReuseBitmapDrawableCache _cache;
		private readonly ConcurrentDictionary<string, ImageInformation> _imageInformations;
		private readonly IMiniLogger _logger;

        private ImageCache(int maxCacheSize, IMiniLogger logger, bool verboseLogging)
		{
			_logger = logger;
			int safeMaxCacheSize = GetMaxCacheSize(maxCacheSize);

            double sizeInMB = Math.Round(safeMaxCacheSize / 1024d / 1024d, 2);
            logger.Debug(string.Format("Image memory cache size: {0} MB", sizeInMB));

			// consider low treshold as a third of maxCacheSize
			int lowTreshold = safeMaxCacheSize / 3;

			_cache = new ReuseBitmapDrawableCache(logger, safeMaxCacheSize, lowTreshold, safeMaxCacheSize, verboseLogging);
			_imageInformations = new ConcurrentDictionary<string, ImageInformation>();
		}

        public static IImageCache Instance
        {
            get
            {
                return _instance ?? (_instance = new ImageCache(ImageService.Instance.Config.MaxMemoryCacheSize, ImageService.Instance.Config.Logger, ImageService.Instance.Config.VerboseMemoryCacheLogging));
            }
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
			_cache.Clear();
			_imageInformations.Clear();
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

		public Tuple<ISelfDisposingBitmapDrawable, ImageInformation> Get(string key)
		{
			ISelfDisposingBitmapDrawable drawable = null;

			if (_cache.TryGetValue(key, out drawable))
			{
				var imageInformation = GetInfo(key);
				return new Tuple<ISelfDisposingBitmapDrawable, ImageInformation>(drawable, imageInformation);
			}

			return null;
		}

		public void Add(string key, ImageInformation imageInformation, ISelfDisposingBitmapDrawable bitmap)
		{
			if (string.IsNullOrWhiteSpace(key) || bitmap == null || bitmap.Handle == IntPtr.Zero || !bitmap.HasValidBitmap || _cache.ContainsKey(key))
				return;

			_imageInformations.TryAdd(key, imageInformation);
			_cache.Add(key, bitmap);
		}

		public void Remove(string key)
		{
            if (ImageService.Instance.Config.VerboseMemoryCacheLogging)
			    _logger.Debug (string.Format ("Called remove from memory cache for '{0}'", key));
			_cache.Remove(key);
			ImageInformation imageInformation;
			_imageInformations.TryRemove(key, out imageInformation);
		}

		public void RemoveSimilar(string baseKey)
		{
			var keysToRemove = _imageInformations.Where(i => i.Value?.BaseKey == baseKey).Select(i => i.Value.CacheKey).ToList();
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
		public ISelfDisposingBitmapDrawable GetBitmapDrawableFromReusableSet(BitmapFactory.Options options)
		{
			if (_cache.Count == 0)
				return null;

			return _cache.GetReusableBitmapDrawable(options.OutWidth, options.OutHeight, options.InPreferredConfig, options.InSampleSize);
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