using System;
using System.Threading;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Widget;
using Object = Java.Lang.Object;
using System.Threading.Tasks;
using HGR.Mobile.Droid.ImageLoading;
using HGR.Mobile.Droid.ImageLoading.Helpers;
using HGR.Mobile.Droid.ImageLoading.Cache;
using HGR.Mobile.Droid.ImageLoading.Extensions;
using HGR.Mobile.Droid.ImageLoading.Drawables;
using System.IO;
using HGR.Mobile.Droid.ImageLoading.IO;

namespace HGR.Mobile.Droid.ImageLoading.Work
{
	public class ImageLoaderTask
	{
        public enum ImageSource
        {
            Filepath,
            Url
        }

		public EventHandler OnComplete;

		protected readonly Context _context;
		protected CancellationTokenSource CancellationToken;
		private const int FADE_TRANSITION_MILISECONDS = 50;
		private readonly WeakReference<ImageView> _imageWeakReference;
		private readonly BitmapFactory.Options _options;

        private readonly ImageSource _source;
		private readonly string _path;
        private readonly int _resampleWidth;
        private readonly int _resampleHeight;

        public ImageLoaderTask(string path, ImageView imageView, ImageSource source = ImageSource.Filepath, int resampleWidth = -1, int resampleHeight = -1)
		{
			CancellationToken = new CancellationTokenSource();
            _context = Android.App.Application.Context.ApplicationContext;

			_path = path;
            _source = source;
            _resampleWidth = resampleWidth;
            _resampleHeight = resampleHeight;
			_imageWeakReference = new WeakReference<ImageView>(imageView);

			_options = new BitmapFactory.Options()
			{
				InPurgeable = true
			};

			UseFadeInBitmap = true;
		}

		public bool Completed { get; set; }

		public virtual string Key
		{
			get
			{
				return _path;
			}
		}

		public bool UseFadeInBitmap { get; set; }

		/// <summary>
		/// Calculate an inSampleSize for use in a {@link android.graphics.BitmapFactory.Options} object when decoding
		/// the closest inSampleSize that is a power of 2 and will result in the final decoded bitmap
		/// </summary>
		/// <param name="options"></param>
		/// <param name="reqWidth"></param>
		/// <param name="reqHeight"></param>
		/// <returns></returns>
		public static int CalculateInSampleSize(BitmapFactory.Options options,
		                                        int reqWidth, int reqHeight)
		{
			// BEGIN_INCLUDE (calculate_sample_size)
			// Raw height and width of image
			int height = options.OutHeight;
			int width = options.OutWidth;
			int inSampleSize = 1;

			if (height > reqHeight || width > reqWidth)
			{
				int halfHeight = height/2;
				int halfWidth = width/2;

				// Calculate the largest inSampleSize value that is a power of 2 and keeps both
				// height and width larger than the requested height and width.
				while ((halfHeight/inSampleSize) > reqHeight
				       && (halfWidth/inSampleSize) > reqWidth)
				{
					inSampleSize *= 2;
				}

				// This offers some additional logic in case the image has a strange
				// aspect ratio. For example, a panorama may have a much larger
				// width than height. In these cases the total pixels might still
				// end up being too large to fit comfortably in memory, so we should
				// be more aggressive with sample down the image (=larger inSampleSize).

				long totalPixels = width*height/inSampleSize;

				// Anything more than 2x the requested pixels we'll sample down further
				long totalReqPixelsCap = reqWidth*reqHeight*2;

				while (totalPixels > totalReqPixelsCap)
				{
					inSampleSize *= 2;
					totalPixels /= 2;
				}
			}
			return inSampleSize;
		}

		public void Cancel()
		{
			CancellationToken.Cancel();
            MiniLogger.Debug(string.Format("Canceled image generation for {0}", Key));
		}

        public bool IsCancelled
        {
            get
            {
                return CancellationToken.IsCancellationRequested;
            }
        }

		/// <summary>
		/// Once the image is processed, associates it to the imageView
		/// </summary>
        public async Task RunAsync()
		{
            try {
                if (Completed || CancellationToken.IsCancellationRequested || ImageService.ExitTasksEarly)
                    return;

                BitmapDrawable drawable = null;
                try
                {
                    drawable = await RetrieveDrawableAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    MiniLogger.Error("An error occured while retrieving drawable.", ex);
                    return;
                }

                if (drawable == null)
                    return;

                var imageView = GetAttachedImageView();
                if (imageView == null)
                    return;

                if (CancellationToken.IsCancellationRequested)
                    return;

                // Post on main thread
                Handler handler = new Handler(Looper.MainLooper);
                handler.Post(() =>
                    {
                        if (CancellationToken.IsCancellationRequested)
                            return;

                        SetImageDrawable(imageView, drawable);
                        Completed = true;
                        if (OnComplete != null)
                            OnComplete(this, EventArgs.Empty);
                    });
            } finally {
                ImageService.RemovePendingTask(this);
            }
		}

		// bitmaps using the decode* methods from {@link android.graphics.BitmapFactory}. This implementation calculates
		// having a width and height equal to or larger than the requested width and height.

        protected async Task<BitmapDrawable> RetrieveDrawableAsync()
		{
            // If the image cache is available and this task has not been cancelled by another
            // thread and the ImageView that was originally bound to this task is still bound back
            // to this task and our "exit early" flag is not set then try and fetch the bitmap from
            // the cache
            if (CancellationToken.IsCancellationRequested || GetAttachedImageView() == null || ImageService.ExitTasksEarly)
                return null;

            BitmapDrawable drawable = await GetDrawableAsync(_path).ConfigureAwait(false);
            if (drawable == null)
                return null;

            // FMT: even if it was canceled, if we have the bitmap we add it to the cache
            ImageCache.Instance.Add(_path, drawable);

            return drawable;
        }

		private static void AddInBitmapOptions(BitmapFactory.Options options)
		{
			// inBitmap only works with mutable bitmaps so force the decoder to
			// return mutable bitmaps.
			options.InMutable = true;


			// Try and find a bitmap to use for inBitmap
			var inBitmap = ImageCache.Instance.GetBitmapFromReusableSet(options);

			if (inBitmap != null)
			{
				options.InBitmap = inBitmap;
			}
		}

		private ImageView GetAttachedImageView()
		{
			ImageView imageView;
			_imageWeakReference.TryGetTarget(out imageView);

			var task = imageView.GetImageLoaderTask();

			return this == task
				? imageView
				: null;
		}

        protected virtual async Task<BitmapDrawable> GetDrawableAsync(string path)
		{
            if (CancellationToken.IsCancellationRequested)
                return null;

            byte[] bytes = null;

            try
            {
                switch (_source)
                {
                    case ImageSource.Filepath:
                        bytes = await FileStore.ReadBytes(path).ConfigureAwait(false);
                        break;
                    case ImageSource.Url:
                        bytes = await new DownloadCache().GetAsync(path).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                MiniLogger.Error("Unable to retrieve image data", ex);
                return null;
            }

            if (bytes == null || bytes.Length <= 0)
				return null;

            // First decode with inJustDecodeBounds=true to check dimensions
			var options = new BitmapFactory.Options
			{
				InJustDecodeBounds = true
			};

            if (CancellationToken.IsCancellationRequested)
                return null;

            return await Task.Run(() =>
            {
                if (CancellationToken.IsCancellationRequested)
                    return null;

                try
                {
                    BitmapFactory.DecodeFile(path, options);
                    if (_resampleWidth > 0)
                        options.OutWidth = _resampleWidth;
                    if (_resampleHeight > 0)
                        options.OutHeight = _resampleHeight;
                }
                catch (Exception ex)
                {
                    MiniLogger.Error("Something wrong happened while asynchronously retrieving image size from file: " + path, ex);
                    return null;
                }
                
                if (CancellationToken.IsCancellationRequested)
                    return null;

                try
                {
        			// Calculate inSampleSize
        			options.InSampleSize = CalculateInSampleSize(options, options.OutWidth, options.OutHeight);
                
        			// If we're running on Honeycomb or newer, try to use inBitmap
        			if (Utils.HasHoneycomb())
        				AddInBitmapOptions(_options);
                }
                catch (Exception ex)
                {
                    MiniLogger.Error("Something wrong happened while adding decoding options to image: " + path, ex);
                }

                if (CancellationToken.IsCancellationRequested)
                    return null;
                
                Bitmap bitmap;
                try
                {
                    bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, _options);
                }
                catch (Exception ex)
                {
                    MiniLogger.Error("Something wrong happened while asynchronously loading/decoding image: " + path, ex);
                    return null;
                }

                if (bitmap == null || CancellationToken.IsCancellationRequested)
                    return null;
                
                if (Utils.HasHoneycomb())
                {
                    // Running on Honeycomb or newer, so wrap in a standard BitmapDrawable
                    return new BitmapDrawable(_context.Resources, bitmap);
                }
                else
                {
                    // Running on Gingerbread or older, so wrap in a RecyclingBitmapDrawable
                    // which will recycle automagically
                    return new ManagedBitmapDrawable(_context.Resources, bitmap);
                }
            }).ConfigureAwait(false);
		}

		private void SetImageDrawable(ImageView imageView, Drawable drawable)
		{
			imageView.LayoutParameters.Height = drawable.IntrinsicHeight;
			imageView.LayoutParameters.Width = drawable.IntrinsicWidth;

			if (UseFadeInBitmap)
			{
                var drawables = new[]
                {
                    new ColorDrawable(Color.Transparent),
                    drawable
                };

                var td = new TransitionDrawable(drawables);
                imageView.SetImageDrawable(td);
                td.StartTransition(FADE_TRANSITION_MILISECONDS);
                //imageView.SetImageDrawable(drawable);
			}
			else
			{
				imageView.SetImageDrawable(drawable);
			}
		}
	}

	public class AsyncDrawable : BitmapDrawable
	{
		private readonly WeakReference<ImageLoaderTask> _imageLoaderTaskReference;

		public AsyncDrawable(Resources res, Bitmap bitmap, ImageLoaderTask imageLoaderTask)
			: base(res, bitmap)
		{
			_imageLoaderTaskReference = new WeakReference<ImageLoaderTask>(imageLoaderTask);
		}

		public ImageLoaderTask GetImageLoaderTask()
		{
			ImageLoaderTask task;
			_imageLoaderTaskReference.TryGetTarget(out task);
			return task;
		}
	}
}