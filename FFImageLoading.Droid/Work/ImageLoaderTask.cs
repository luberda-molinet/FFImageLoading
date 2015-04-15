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
using System.IO;
using FFImageLoading.Work;
using FFImageLoading.Helpers;
using FFImageLoading.IO;
using FFImageLoading.Cache;
using FFImageLoading.Drawables;
using FFImageLoading.Extensions;

namespace FFImageLoading.Work
{
	public class ImageLoaderTask : ImageLoaderTaskBase
	{
		private const int FADE_TRANSITION_MILISECONDS = 50;
		private readonly WeakReference<ImageView> _imageWeakReference;

		public ImageLoaderTask(IDownloadCache downloadCache, IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, TaskParameter parameters, ImageView imageView)
			: base(mainThreadDispatcher, miniLogger, parameters)
		{
			CancellationToken = new CancellationTokenSource();
			Context = Android.App.Application.Context.ApplicationContext;
			DownloadCache = downloadCache;
			_imageWeakReference = new WeakReference<ImageView>(imageView);

			UseFadeInBitmap = true;
		}

		/// <summary>
		/// This constructor is useful for child classes only. It can help when having a totally different loading logic.
		/// </summary>
		/// <param name="miniLogger">Logger</param>
		/// <param name="key">Key.</param>
		/// <param name="imageView">Image view.</param>
		protected ImageLoaderTask(IDownloadCache downloadCache, IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, string key, ImageView imageView)
			: this(downloadCache, mainThreadDispatcher, miniLogger, TaskParameter.FromFile(key), imageView)
		{
		}

		/// <summary>
		/// Prepares the instance before it runs.
		/// </summary>
		public override async Task PrepareAsync()
		{
			ImageView imageView;
			_imageWeakReference.TryGetTarget(out imageView);
			if (imageView == null)
				return;
			
			// Cancel current task attached to the same image view, if needed only
			var currentAssignedTask = imageView.GetImageLoaderTask();
			if (currentAssignedTask != null)
			{
				Logger.Debug("Cancel current task attached to the same image view");
				currentAssignedTask.CancelIfNeeded();
			}

			// Assign the Drawable to the image
			var resources = Android.App.Application.Context.ApplicationContext.Resources;
			var drawable = new AsyncDrawable(resources, null, this);
			imageView.SetImageDrawable(drawable);

			// This should probably be reworked at some point so we don't create a dummy AsyncDrawable
			await LoadPlaceHolderAsync(Parameters.LoadingPlaceholderPath, Parameters.LoadingPlaceholderSource).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets or sets a value indicating whether a fade in transition is used to show the image.
		/// </summary>
		/// <value><c>true</c> if a fade in transition is used; otherwise, <c>false</c>.</value>
		public bool UseFadeInBitmap { get; set; }

		protected IDownloadCache DownloadCache { get; private set; }

		protected Context Context { get; private set; }

		/// <summary>
		/// Runs the image loading task: gets image from file, url, asset or cache. Then assign it to the imageView.
		/// </summary>
		public override async Task RunAsync()
		{
			try
			{
				if (Completed || CancellationToken.IsCancellationRequested || ImageService.ExitTasksEarly)
					return;
				
				BitmapDrawable drawable = null;
				try
				{
					drawable = await RetrieveDrawableAsync(Parameters.Path, Parameters.Source, false).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Logger.Error("An error occured while retrieving drawable.", ex);
					Parameters.OnError(ex);
					drawable = null;
				}
					
				if (drawable == null)
				{
					await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
					return;
				}

				Exception trappedException = null;
				try
				{
					var imageView = GetAttachedImageView();
					if (imageView == null)
						return;

					if (CancellationToken.IsCancellationRequested)
						return;

					// Post on main thread
					await MainThreadDispatcher.PostAsync(() =>
					{
						if (CancellationToken.IsCancellationRequested)
							return;

						SetImageDrawable(imageView, drawable, UseFadeInBitmap);
						Completed = true;
						Parameters.OnSuccess();
					}).ConfigureAwait(false);
				}
				catch (Exception ex2)
				{
					trappedException = ex2; // All this stupid stuff is necessary to compile with c# 5, since we can't await in a catch block...
				}

				// All this stupid stuff is necessary to compile with c# 5, since we can't await in a catch block...
				if (trappedException != null)
				{
					await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
					throw trappedException;
				}

			}
			catch (Exception ex)
			{
				Logger.Error("An error occured", ex);
				Parameters.OnError(ex);
			}
			finally
			{
				ImageService.RemovePendingTask(this);
				Parameters.OnFinish(this);
			}
		}

		/// <summary>
		/// Tries to load requested image from the cache asynchronously.
		/// </summary>
		/// <returns>A boolean indicating if image was loaded from cache.</returns>
		public override async Task<bool> TryLoadingFromCacheAsync()
		{
			var imageView = GetAttachedImageView();
			if (imageView == null)
				return false; // weird situation, dunno what to do

			var value = ImageCache.Instance.Get(Key);
			if (value == null)
				return false; // not available in the cache

			await MainThreadDispatcher.PostAsync(() =>
			{
				imageView.SetImageDrawable(value);
				if (imageView.AdjustViewBounds)
				{
					imageView.LayoutParameters.Height = value.IntrinsicHeight;
					imageView.LayoutParameters.Width = value.IntrinsicWidth;
				}
			}).ConfigureAwait(false);
			return true; // found and loaded from cache
		}

		protected virtual async Task<BitmapDrawable> GetDrawableAsync(string sourcePath, ImageSource source, bool isPlaceHolder)
		{
			if (CancellationToken.IsCancellationRequested)
				return null;

			byte[] bytes = null;
			string path = sourcePath;

			try
			{
				switch (source)
				{
					case ImageSource.ApplicationBundle:
						var stream = Context.Assets.Open(path);
						using (var memory = new MemoryStream())
						{
							await stream.CopyToAsync(memory).ConfigureAwait(false);
							bytes = memory.ToArray();
						}
						break;
					case ImageSource.Filepath:
						bytes = await FileStore.ReadBytes(path).ConfigureAwait(false);
						break;
					case ImageSource.Url:
						var downloadedData = await DownloadCache.GetAsync(path, Parameters.CacheDuration).ConfigureAwait(false);
						bytes = downloadedData.Bytes;
						path = downloadedData.CachedPath;
						break;
				}
			}
			catch (Exception ex)
			{
				Logger.Error("Unable to retrieve image data", ex);
				Parameters.OnError(ex);
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
				}
				catch (Exception ex)
				{
					Logger.Error("Something wrong happened while asynchronously retrieving image size from file: " + path, ex);
					Parameters.OnError(ex);
					return null;
				}

				if (CancellationToken.IsCancellationRequested)
					return null;

				options.InPurgeable = true;
				options.InJustDecodeBounds = false;

				try
				{
					if (Parameters.DownSampleSize != null && (Parameters.DownSampleSize.Item1 > 0 || Parameters.DownSampleSize.Item2 > 0))
					{
						// Calculate inSampleSize
						options.InSampleSize = CalculateInSampleSize(options, (int)Parameters.DownSampleSize.Item1, (int)Parameters.DownSampleSize.Item2);
					}
					
					// If we're running on Honeycomb or newer, try to use inBitmap
					if (Utils.HasHoneycomb())
						AddInBitmapOptions(options);
				}
				catch (Exception ex)
				{
					Logger.Error("Something wrong happened while adding decoding options to image: " + path, ex);
					Parameters.OnError(ex);
				}

				if (CancellationToken.IsCancellationRequested)
					return null;

				Bitmap bitmap;
				try
				{
					bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options);
				}
				catch (Exception ex)
				{
					Logger.Error("Something wrong happened while asynchronously loading/decoding image: " + path, ex);
					Parameters.OnError(ex);
					return null;
				}

				if (bitmap == null || CancellationToken.IsCancellationRequested)
					return null;

				// Running on Honeycomb or newer, so wrap in a standard BitmapDrawable
				if (Utils.HasHoneycomb())
				{
					if (isPlaceHolder)
						return new AsyncDrawable(Context.Resources, bitmap, this);
					else
						return new BitmapDrawable(Context.Resources, bitmap);
				}
				else // Running on Gingerbread or older, so wrap in a RecyclingBitmapDrawable which will recycle automagically
				{
					if (isPlaceHolder)
						return new ManagedAsyncDrawable(Context.Resources, bitmap, this);
					else
						return new ManagedBitmapDrawable(Context.Resources, bitmap);
				}
			}).ConfigureAwait(false);
		}

		/// <summary>
		/// Loads given placeHolder into the imageView.
		/// </summary>
		/// <returns>An awaitable task.</returns>
		/// <param name="placeholderPath">Full path to the placeholder.</param>
		/// <param name="source">Source for the path: local, web, assets</param>
		protected async Task<bool> LoadPlaceHolderAsync(string placeholderPath, ImageSource source)
		{
			if (string.IsNullOrWhiteSpace(placeholderPath))
				return false;

			BitmapDrawable drawable = null;
			try
			{
				drawable = await RetrieveDrawableAsync(placeholderPath, source, true).ConfigureAwait(false);

				if (drawable == null)
					return false;
			}
			catch (Exception ex)
			{
				Logger.Error("An error occured while retrieving drawable.", ex);
				Parameters.OnError(ex);
				return false;
			}
			
			var imageView = GetAttachedImageView();
			if (imageView == null)
				return false;

			if (CancellationToken.IsCancellationRequested)
				return false;

			// Post on main thread but don't wait for it
			MainThreadDispatcher.Post(() =>
				{
					if (CancellationToken.IsCancellationRequested)
						return;

					SetImageDrawable(imageView, drawable, false);
				});

			return true;
		}

		// bitmaps using the decode* methods from {@link android.graphics.BitmapFactory}. This implementation calculates
		// having a width and height equal to or larger than the requested width and height.
		private async Task<BitmapDrawable> RetrieveDrawableAsync(string sourcePath, ImageSource source, bool isPlaceHolder)
		{
			// If the image cache is available and this task has not been cancelled by another
			// thread and the ImageView that was originally bound to this task is still bound back
			// to this task and our "exit early" flag is not set then try and fetch the bitmap from
			// the cache
			if (CancellationToken.IsCancellationRequested || GetAttachedImageView() == null || ImageService.ExitTasksEarly)
				return null;

			BitmapDrawable drawable = await GetDrawableAsync(sourcePath, source, isPlaceHolder).ConfigureAwait(false);
			if (drawable == null)
				return null;

			// FMT: even if it was canceled, if we have the bitmap we add it to the cache
			ImageCache.Instance.Add(sourcePath, drawable);

			return drawable;
		}

		/// <summary>
		/// Calculate an inSampleSize for use in a {@link android.graphics.BitmapFactory.Options} object when decoding
		/// the closest inSampleSize that is a power of 2 and will result in the final decoded bitmap
		/// </summary>
		/// <param name="options"></param>
		/// <param name="reqWidth"></param>
		/// <param name="reqHeight"></param>
		/// <returns></returns>
		private int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
		{
			// Raw height and width of image
			float height = options.OutHeight;
			float width = options.OutWidth;
			double inSampleSize = 1D;

			if (height > reqHeight || width > reqWidth)
			{
				int halfHeight = (int)(height / 2);
				int halfWidth = (int)(width / 2);

				// Calculate a inSampleSize that is a power of 2 - the decoder will use a value that is a power of two anyway.
				while ((halfHeight / inSampleSize) > reqHeight && (halfWidth / inSampleSize) > reqWidth)
				{
					inSampleSize *= 2;
				}
			}

			return (int)inSampleSize;
		}

		private void AddInBitmapOptions(BitmapFactory.Options options)
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

		private void SetImageDrawable(ImageView imageView, Drawable drawable, bool fadeIn)
		{
			if (imageView.AdjustViewBounds)
			{
				imageView.LayoutParameters.Height = drawable.IntrinsicHeight;
				imageView.LayoutParameters.Width = drawable.IntrinsicWidth;
			}

			if (fadeIn)
			{
				var drawables = new[] {
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

	public class AsyncDrawable : BitmapDrawable, IAsyncDrawable
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

	public class ManagedAsyncDrawable : ManagedBitmapDrawable, IAsyncDrawable
	{
		private readonly WeakReference<ImageLoaderTask> _imageLoaderTaskReference;

		public ManagedAsyncDrawable(Resources res, Bitmap bitmap, ImageLoaderTask imageLoaderTask)
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