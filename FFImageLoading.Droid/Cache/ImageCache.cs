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
using FFImageLoading.Drawables;

namespace FFImageLoading.Cache
{
	public class ImageCache : LruCache<BitmapDrawable>, IImageCache
	{
		private ConcurrentDictionary<string, bool> _references;
		private readonly ConcurrentSet<SoftReference> _reusableBitmaps;
		private static IImageCache _instance;

		private ImageCache() : base(GetCacheSizeInPercent(0.6f))
		{
			if (Utils.HasHoneycomb())
                _reusableBitmaps = new ConcurrentSet<SoftReference>();

			_references = new ConcurrentDictionary<string, bool>();
		}

		public static IImageCache Instance
		{
			get
			{
                return _instance ?? (_instance = new ImageCache());
			}
		}

		public int Count { get; set; }

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
			if (_references != null && _references.Count > 0)
				_references.Clear();
			EvictAll();
            // Can't use minilogger here, we would have too many dependencies
            System.Diagnostics.Debug.WriteLine("ImageCache cleared.");
		}

		public void Add(string key, BitmapDrawable bitmap)
		{
            if (string.IsNullOrWhiteSpace(key) || bitmap == null || _references.ContainsKey(key))
				return;


			var managedBitmap = bitmap as ManagedBitmapDrawable;

			if (managedBitmap != null)
			{
				managedBitmap.SetIsCached(true);
			}
			_references.GetOrAdd(key, true);
			Put(key, bitmap);
			Count++;
		}

		public Bitmap GetBitmapFromReusableSet(BitmapFactory.Options options)
		{
			Bitmap bitmap = null;

			var bitmapToRemove = new List<SoftReference>();
			if (_reusableBitmaps != null && !_reusableBitmaps.IsEmpty)
			{
				foreach (var softReference in _reusableBitmaps)
				{
					var item = softReference.Get() as Bitmap;
					if (item != null && item.IsMutable)
					{
						if (CanUseForInBitmap(item, options))
						{
							bitmap = item;
							bitmapToRemove.Add(softReference);
							break;
						}
					}
					else
					{
						bitmapToRemove.Add(softReference);
					}
				}
			}

			if (_reusableBitmaps != null && bitmapToRemove.Count > 0)
			{
				foreach (var i in bitmapToRemove)
					_reusableBitmaps.TryRemove(i);
			}

			return bitmap;
		}

		protected override void EntryRemoved(bool evicted, Object key, Object oldValue, Object newValue)
		{
			var old = oldValue as ManagedBitmapDrawable;
			if (old != null)
			{
				old.SetIsCached(false);
				bool tmp;
				_references.TryRemove((string) key, out tmp);
				Count--;
			}
			else
			{
				if (Utils.HasHoneycomb())
				{
					_reusableBitmaps.TryAdd(new SoftReference((oldValue as BitmapDrawable).Bitmap));
				}
			}
			//var oldBmp = (BitmapDrawable) oldValue;
			//oldBmp.Bitmap.Recycle();
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

			return (int) Math.Round(percent*Runtime.GetRuntime().MaxMemory()/1024);
		}

		private bool CanUseForInBitmap(Bitmap item, BitmapFactory.Options options)
		{
			if (!Utils.HasKitKat())
			{
				// On earlier versions, the dimensions must match exactly and the inSampleSize must be 1
				return item.Width == options.OutWidth && item.Height == options.OutHeight && options.InSampleSize == 1;
			}

			// From Android 4.4 (KitKat) onward we can re-use if the byte size of the new bitmap
			// is smaller than the reusable bitmap candidate allocation byte count.
			int width = options.OutWidth/options.InSampleSize;
			int height = options.OutHeight/options.InSampleSize;
			int byteCount = width*height*GetBytesPerPixel(item.GetConfig());
			return byteCount <= item.AllocationByteCount;
		}

		//protected override int SizeOf(Object key, Object value)
		//{
		//	var size= GetBitmapSize((BitmapDrawable) value)/1024;

		//	return size == 0
		//		? 1
		//		: size;
		//}
	}
}