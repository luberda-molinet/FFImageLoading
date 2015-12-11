using System.Collections.Generic;
using Android.Graphics;
using Android.Graphics.Drawables;
using Java.Lang;
using Java.Lang.Ref;
using Exception = System.Exception;
using Math = System.Math;
using System.Collections.Concurrent;
using FFImageLoading.Collections;
using FFImageLoading.Helpers;
using Android.Content;
using Android.App;
using Android.Content.PM;

namespace FFImageLoading.Cache
{
	public class ImageCache : LruCache<BitmapDrawable>, IImageCache
	{
		private ConcurrentDictionary<string, bool> _references;
		private readonly ConcurrentSet<WeakReference> _reusableBitmaps;
		private static IImageCache _instance;
		private static object _clearLock;

        private ImageCache(int maxCacheSize) : base(GetMaxCacheSize())
		{
			if (Utils.HasHoneycomb())
				_reusableBitmaps = new ConcurrentSet<WeakReference>();

			_references = new ConcurrentDictionary<string, bool>();
			_clearLock = new object();
		}

        public static IImageCache Instance
        {
            get
            {
                return _instance ?? (_instance = new ImageCache(ImageService.Config.MaxCacheSize));
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
			bool didClean = false;

			lock (_clearLock)
			{
				if (_reusableBitmaps != null && _reusableBitmaps.Count > 0)
				{
					foreach (var weakReference in _reusableBitmaps)
					{
						var item = weakReference.Get() as Bitmap;
						if (item != null && item.Handle != System.IntPtr.Zero && !item.IsRecycled)
						{
							// Here it is safe to recycle, these items aren't supposed to be displayed anymore
							item.Recycle();
							item.Dispose();
						}
					}

					_reusableBitmaps.Clear();
					didClean = true;
				}

				if (Size() > 0)
				{
					if (_references != null && _references.Count > 0)
					{
						foreach (var pair in _references)
						{
							var drawable = Get(pair.Key) as BitmapDrawable;
							if (drawable != null && drawable.Handle != System.IntPtr.Zero)
							{
								var item = drawable.Bitmap;
								if (item != null && item.Handle != System.IntPtr.Zero && !item.IsRecycled)
								{
									item.Dispose();
								}
								drawable.Dispose();
							}
						}
					}

					EvictAll();
					didClean = true;
				}

				if (_references != null && _references.Count > 0)
				{
					_references.Clear();
				}

				if (didClean)
				{
					// Force immediate Garbage collection. Please note that is resource intensive.
					System.GC.Collect();
					System.GC.WaitForPendingFinalizers ();
					System.GC.WaitForPendingFinalizers (); // Double call since GC doesn't always find resources to be collected: https://bugzilla.xamarin.com/show_bug.cgi?id=20503
					System.GC.Collect ();
				}
			}

			if (didClean)
			{
				// Can't use minilogger here, we would have too many dependencies
				System.Diagnostics.Debug.WriteLine("ImageCache cleared and forcing immediate garbage collection.");
			}
		}

		public void Add(string key, BitmapDrawable bitmap)
		{
            if (string.IsNullOrWhiteSpace(key) || bitmap == null || _references.ContainsKey(key))
				return;
			
			_references.GetOrAdd(key, true);
			Put(key, bitmap);
		}

		public void Remove(string key)
		{
			base.Remove(key);
		}

		public Bitmap GetBitmapFromReusableSet(BitmapFactory.Options options)
		{
			if (_reusableBitmaps != null && !_reusableBitmaps.IsEmpty)
			{
				foreach (var weakReference in _reusableBitmaps)
				{
					bool removed = _reusableBitmaps.TryRemove(weakReference);
					var item = weakReference.Get() as Bitmap;

					if (item != null && item.Handle != System.IntPtr.Zero && item.IsMutable)
					{
						if (CanUseForInBitmap(item, options))
						{
							// reuse the bitmap
							return item;
						}
						else
						{
							if (removed)
							{
								// the bitmap isn't usable yet, put it back in reusableBitmaps
								_reusableBitmaps.TryAdd(weakReference);
							}
						}
					}
					else
					{
						item.Recycle();
						item.Dispose();
					}
				}
			}

			return null;
		}

		protected override void EntryRemoved(bool evicted, Object key, Object oldValue, Object newValue)
		{
			var drawable = oldValue as BitmapDrawable;
			if (drawable == null)
				return;

			var bitmap = drawable.Bitmap;
			if (bitmap != null && bitmap.Handle != System.IntPtr.Zero)
			{
				if (Utils.HasHoneycomb())
				{
					_reusableBitmaps.TryAdd(new WeakReference(bitmap));
				}
			}

			// We need to inform .NET GC that the Bitmap is no longer in use in .NET world.
			// It might get reused by Java world, and we can access it later again since we still have a Java weakreference to it
			drawable.Dispose();
			bitmap.Dispose();
		}

		/// <summary>
		/// Return the byte usage per pixel of a bitmap based on its configuration.
		/// </summary>
		/// <param name="config">The bitmap configuration</param>
		/// <returns>The byte usage per pixel</returns>
		private static int GetBytesPerPixel(Bitmap.Config config)
		{
			if (config == Bitmap.Config.Argb8888)
			{
				return 4;
			}
			else if (config == Bitmap.Config.Rgb565)
			{
				return 2;
			}
			else if (config == Bitmap.Config.Argb4444)
			{
				return 2;
			}
			else if (config == Bitmap.Config.Alpha8)
			{
				return 1;
			}
			return 1;
		}

        private static int GetMaxCacheSize()
        {
            if (ImageService.Config.MaxCacheSize <= 0)
                return GetCacheSizeInPercent(0.2f); // 20%

            return Math.Min(GetCacheSizeInPercent(0.2f), ImageService.Config.MaxCacheSize);
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

			var context = Android.App.Application.Context.ApplicationContext;
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

		private bool CanUseForInBitmap(Bitmap item, BitmapFactory.Options options)
		{
			if (!Utils.HasKitKat())
			{
				// On earlier versions, the dimensions must match exactly and the inSampleSize must be 1
				return item.Width == options.OutWidth && item.Height == options.OutHeight && options.InSampleSize == 1;
			}

			if (options.InSampleSize == 0)
				options.InSampleSize = 1; // to avoid division by zero
			
			// From Android 4.4 (KitKat) onward we can re-use if the byte size of the new bitmap
			// is smaller than the reusable bitmap candidate allocation byte count.
			int width = options.OutWidth/options.InSampleSize;
			int height = options.OutHeight/options.InSampleSize;
			int byteCount = width*height*GetBytesPerPixel(item.GetConfig());
			return byteCount <= item.AllocationByteCount;
		}

		protected override int SizeOf(Object key, Object value)
		{
		    var size = GetBitmapSize((BitmapDrawable) value);

			return size == 0 ? 1 : size;
		}
	}
}