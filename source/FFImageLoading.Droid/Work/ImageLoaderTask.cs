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
using FFImageLoading.Extensions;
using Android.Runtime;
using System;
using FFImageLoading.Work.StreamResolver;
using System.Linq;
using FFImageLoading.Drawables;

namespace FFImageLoading.Work
{
	public class ImageLoaderTask : ImageLoaderTaskBase
	{
		private const float FADE_TRANSITION_MILISECONDS = 400f;
		private static object _decodingLock = new object();

		private readonly WeakReference<ImageView> _imageWeakReference;
		private WeakReference<Drawable> _loadingPlaceholderWeakReference;

		public ImageLoaderTask(IDownloadCache downloadCache, IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, TaskParameter parameters, ImageView imageView)
			: base(mainThreadDispatcher, miniLogger, parameters)
		{
			DownloadCache = downloadCache;
			_imageWeakReference = new WeakReference<ImageView>(imageView);
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
		/// Indicates if the task uses the same native control
		/// </summary>
		/// <returns><c>true</c>, if same native control is used, <c>false</c> otherwise.</returns>
		/// <param name="task">Task to check.</param>
		public override bool UsesSameNativeControl(IImageLoaderTask task)
		{
			var loaderTask = task as ImageLoaderTask;
			if (loaderTask == null)
				return false;
			return UsesSameNativeControl(loaderTask);
		}

		private bool UsesSameNativeControl(ImageLoaderTask task)
		{
			ImageView currentControl;
			_imageWeakReference.TryGetTarget(out currentControl);

			ImageView control;
			task._imageWeakReference.TryGetTarget(out control);
			if (currentControl == null || control == null || currentControl.Handle == IntPtr.Zero || control.Handle == IntPtr.Zero)
				return false;

			return currentControl.Handle == control.Handle;
		}

		/// <summary>
		/// Prepares the instance before it runs.
		/// </summary>
		public override async Task<bool> PrepareAndTryLoadingFromCacheAsync()
		{
			ImageView imageView;
			_imageWeakReference.TryGetTarget(out imageView);
			if (imageView == null)
				return false;
			
			var cacheResult = await TryLoadingFromCacheAsync(imageView).ConfigureAwait(false);
			if (cacheResult == CacheResult.Found || cacheResult == CacheResult.ErrorOccured) // If image is loaded from cache there is nothing to do here anymore, if something weird happened with the cache... error callback has already been called, let's just leave
				return true; // stop processing if loaded from cache OR if loading from cached raised an exception

			if (IsCancelled)
				return true; // stop processing if cancelled

			bool hasDrawable = await LoadPlaceHolderAsync(Parameters.LoadingPlaceholderPath, Parameters.LoadingPlaceholderSource, imageView, true).ConfigureAwait(false);
			if (!hasDrawable)
			{
				// Assign the Drawable to the image
				var drawable = new AsyncDrawable(Context.Resources, null, this);
				await MainThreadDispatcher.PostAsync(() =>
					{
						if (imageView == null || imageView.Handle == IntPtr.Zero)
							return;

						imageView.SetImageDrawable(drawable);
					}).ConfigureAwait(false);
			}

			return false;
		}

		/// <summary>
		/// Gets or sets a value indicating whether a fade in transition is used to show the image.
		/// </summary>
		/// <value><c>true</c> if a fade in transition is used; otherwise, <c>false</c>.</value>
		public bool UseFadeInBitmap 
		{ 
			get
			{
				return Parameters.FadeAnimationEnabled.HasValue ? 
					Parameters.FadeAnimationEnabled.Value : ImageService.Config.FadeAnimationEnabled;
			}
		}

		protected IDownloadCache DownloadCache { get; private set; }

		protected Context Context
		{
			get
			{
				return Android.App.Application.Context.ApplicationContext;
			}
		}

		/// <summary>
		/// Runs the image loading task: gets image from file, url, asset or cache. Then assign it to the imageView.
		/// </summary>
		protected override async Task<GenerateResult> TryGeneratingImageAsync()
		{
			WithLoadingResult<SelfDisposingBitmapDrawable> drawableWithResult = null;
			if (!string.IsNullOrWhiteSpace(Parameters.Path))
			{
				try
				{
					drawableWithResult = await RetrieveDrawableAsync(Parameters.Path, Parameters.Source, false).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Logger.Error("An error occured while retrieving drawable.", ex);
					drawableWithResult = null;
				}
			}

			var imageView = GetAttachedImageView();
			if (imageView == null)
				return GenerateResult.InvalidTarget;

			if (drawableWithResult == null)
			{
				// Show error placeholder
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource, imageView, false).ConfigureAwait(false);
				return GenerateResult.Failed;
			}

			Exception trappedException = null;
			try
			{
				if (CancellationToken.IsCancellationRequested)
					return GenerateResult.Canceled;

				// Post on main thread
				await MainThreadDispatcher.PostAsync(() =>
					{
						if (CancellationToken.IsCancellationRequested)
							return;
						
						if (imageView == null || imageView.Handle == IntPtr.Zero)
							return;
						
						SetImageDrawable(imageView, drawableWithResult.Item, UseFadeInBitmap);
						
						Completed = true;
						Parameters.OnSuccess(new ImageSize(drawableWithResult.Item.IntrinsicWidth, drawableWithResult.Item.IntrinsicHeight), drawableWithResult.Result);
					}).ConfigureAwait(false);

				if (!Completed)
					return GenerateResult.Failed;
			}
			catch (Exception ex2)
			{
				trappedException = ex2; // All this stupid stuff is necessary to compile with c# 5, since we can't await in a catch block...
			}

			// All this stupid stuff is necessary to compile with c# 5, since we can't await in a catch block...
			if (trappedException != null)
			{
				// Show error placeholder
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource, imageView, false).ConfigureAwait(false);
				throw trappedException;
			}

			return GenerateResult.Success;
		}

		/// <summary>
		/// Tries to load requested image from the cache asynchronously.
		/// </summary>
		/// <returns>A boolean indicating if image was loaded from cache.</returns>
		public override Task<CacheResult> TryLoadingFromCacheAsync()
		{
			var imageView = GetAttachedImageView();
			return TryLoadingFromCacheAsync(imageView);
		}

//		public async Task<Bitmap> LoadFromStream(Stream stream)
//		{
//			// Otherwise load image normally
//			var drawableWithResult = await GetDrawableAsync(Parameters.Path, Parameters.Source, false).ConfigureAwait(false);
//			if (drawableWithResult == null || drawableWithResult.Item == null)
//				return null;
//
//			// If we have the bitmap we add it to the cache
//			ImageCache.Instance.Add(GetKey(), drawableWithResult.Item);
//
//			return drawableWithResult.Item.Bitmap;
//		}

		/// <summary>
		/// Loads the image from given stream asynchronously.
		/// </summary>
		/// <returns>An awaitable task.</returns>
		/// <param name="stream">The stream to get data from.</param>
		public override async Task<GenerateResult> LoadFromStreamAsync(Stream stream)
		{
			if (stream == null)
				return GenerateResult.Failed;

			if (CancellationToken.IsCancellationRequested)
				return GenerateResult.Canceled;

			var imageView = GetAttachedImageView();
			if (imageView == null)
				return GenerateResult.InvalidTarget;

			var resultWithDrawable = await GetDrawableAsync("Stream", ImageSource.Stream, false, stream).ConfigureAwait(false);
			if (resultWithDrawable == null || resultWithDrawable.Item == null)
			{
				// Show error placeholder
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource, imageView, false).ConfigureAwait(false);

				return GenerateResult.Failed;
			}

			Exception trappedException = null;
			try
			{
				if (CancellationToken.IsCancellationRequested)
					return GenerateResult.Canceled;

				// Post on main thread
				await MainThreadDispatcher.PostAsync(() =>
					{
						if (CancellationToken.IsCancellationRequested)
							return;

						if (imageView == null || imageView.Handle == IntPtr.Zero)
							return;
						
						SetImageDrawable(imageView, resultWithDrawable.Item, UseFadeInBitmap);
						
						Completed = true;
						Parameters.OnSuccess(new ImageSize(resultWithDrawable.Item.IntrinsicWidth, resultWithDrawable.Item.IntrinsicHeight), resultWithDrawable.Result);
					}).ConfigureAwait(false);

				if (!Completed)
					return GenerateResult.Failed;
			}
			catch (Exception ex2)
			{
				trappedException = ex2; // All this stupid stuff is necessary to compile with c# 5, since we can't await in a catch block...
			}

			// All this stupid stuff is necessary to compile with c# 5, since we can't await in a catch block...
			if (trappedException != null)
			{
				// Show error placeholder
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource, imageView, false).ConfigureAwait(false);
				throw trappedException;
			}

			return GenerateResult.Success;
		}

		protected virtual async Task<WithLoadingResult<SelfDisposingBitmapDrawable>> GetDrawableAsync(string path, ImageSource source, bool isLoadingPlaceHolder, Stream originalStream = null)
		{
			if (CancellationToken.IsCancellationRequested)
				return null;

			return await Task.Run<WithLoadingResult<SelfDisposingBitmapDrawable>>(async() =>
				{
					if (CancellationToken.IsCancellationRequested)
						return null;

					// First decode with inJustDecodeBounds=true to check dimensions
					var options = new BitmapFactory.Options
					{
						InJustDecodeBounds = true
					};

					Stream stream = null;
					WithLoadingResult<Stream> streamWithResult = null;
					if (originalStream != null)
					{
						streamWithResult = new WithLoadingResult<Stream>(originalStream, LoadingResult.Stream);
					}
					else
					{
						streamWithResult = await GetStreamAsync(path, source).ConfigureAwait(false);
					}

					if (streamWithResult == null)
					{
						return null;
					}

					if (streamWithResult.Item == null)
					{
						if (streamWithResult.Result == LoadingResult.NotFound)
						{
							Logger.Error(string.Format("Not found: {0} from {1}", path, source.ToString()));
						}
						return null;
					}

					stream = streamWithResult.Item;

					try
					{
						try
						{
							if (streamWithResult.Result == LoadingResult.Internet)
							{
								// When loading from internet stream we shouldn't block otherwise other downloads will be paused
								BitmapFactory.DecodeStream(stream, null, options);
							}
							else
							{
								lock (_decodingLock)
								{
									BitmapFactory.DecodeStream(stream, null, options);
								}
							}

							if (!stream.CanSeek)
							{
								if (originalStream != null)
								{
									// If we cannot seek the original stream then there's not much we can do
									return null;
								}
								else
								{
									// Assets stream can't be seeked to origin position
									stream.Dispose();
									streamWithResult = await GetStreamAsync(path, source).ConfigureAwait(false);
									stream = streamWithResult == null ? null : streamWithResult.Item;

									if (stream == null)
										return null;
								}
							}
							else
							{
								stream.Seek(0, SeekOrigin.Begin);
							}
						}
						catch (Exception ex)
						{
							Logger.Error("Something wrong happened while asynchronously retrieving image size from file: " + path, ex);
							return null;
						}

						if (CancellationToken.IsCancellationRequested)
							return null;

						options.InPurgeable = true;
						options.InJustDecodeBounds = false;

						if (!ImageService.Config.LoadWithTransparencyChannel || Parameters.LoadTransparencyChannel == null || !Parameters.LoadTransparencyChannel.Value)
						{
							// Same quality but no transparency channel. This allows to save 50% of memory: 1 pixel=2bytes instead of 4.
							options.InPreferredConfig = Bitmap.Config.Rgb565;
						}

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
						}

						if (CancellationToken.IsCancellationRequested)
							return null;

						Bitmap bitmap;
						try
						{
							if (streamWithResult.Result == LoadingResult.Internet)
							{
								// When loading from internet stream we shouldn't block otherwise other downloads will be paused
								bitmap = BitmapFactory.DecodeStream(stream, null, options);
							}
							else
							{
								lock (_decodingLock)
								{
									bitmap = BitmapFactory.DecodeStream(stream, null, options);
								}
							}
						}
						catch (Java.Lang.Throwable vme)
						{
							if (vme.Class == Java.Lang.Class.FromType(typeof(Java.Lang.OutOfMemoryError)))
							{
								ImageCache.Instance.Clear(); // Clear will also force a Garbage collection
							}
							return null;
						}
						catch (Exception ex)
						{
							Logger.Error("Something wrong happened while asynchronously loading/decoding image: " + path, ex);
							return null;
						}

						try
						{
							if (bitmap == null || CancellationToken.IsCancellationRequested)
								return null;

							if (Parameters.Transformations != null && Parameters.Transformations.Count > 0)
							{
								foreach (var transformation in Parameters.Transformations.ToList() /* to prevent concurrency issues */)
								{
									if (CancellationToken.IsCancellationRequested)
										return null;

									try
									{
										var old = bitmap;

										// Applying a transformation is both CPU and memory intensive
										lock (_decodingLock)
										{
											var bitmapHolder = transformation.Transform(new BitmapHolder(bitmap));
											bitmap = bitmapHolder.ToNative();
										}

										// Transformation succeeded, so garbage the source
										old.Recycle();
										old.Dispose();
									}
									catch (Exception ex)
									{
										Logger.Error("Can't apply transformation " + transformation.Key + " to image " + path, ex);
									}
								}
							}

							if (isLoadingPlaceHolder)
							{
								return WithLoadingResult.Encapsulate<SelfDisposingBitmapDrawable>(new SelfDisposingAsyncDrawable(Context.Resources, bitmap, this), streamWithResult.Result);
							}
							else
							{
								Drawable placeholderDrawable = null;
								if (_loadingPlaceholderWeakReference != null)
								{
									_loadingPlaceholderWeakReference.TryGetTarget(out placeholderDrawable);
								}

								return WithLoadingResult.Encapsulate<SelfDisposingBitmapDrawable>(new FFBitmapDrawable(Context.Resources, bitmap, placeholderDrawable, FADE_TRANSITION_MILISECONDS, UseFadeInBitmap), streamWithResult.Result);
							}
						}
						finally
						{
							//if (bitmap != null)
							//	bitmap.Dispose(); // .NET space no longer needs to care about the Bitmap. It should exist in Java world only so we break the relationship .NET/Java for the object.
						}
					}
					finally
					{
						if (stream != null)
							stream.Dispose();
					}
				});
		}

		/// <summary>
		/// Loads given placeHolder into the imageView.
		/// </summary>
		/// <returns>An awaitable task.</returns>
		/// <param name="placeholderPath">Full path to the placeholder.</param>
		/// <param name="source">Source for the path: local, web, assets</param>
		protected async Task<bool> LoadPlaceHolderAsync(string placeholderPath, ImageSource source, ImageView imageView, bool isLoadingPlaceholder)
		{
			if (string.IsNullOrWhiteSpace(placeholderPath))
				return false;

			if (imageView == null)
				return false;

			BitmapDrawable drawable = ImageCache.Instance.Get(GetKey(placeholderPath));

			if (drawable != null && drawable.Bitmap != null && drawable.Bitmap.Handle != IntPtr.Zero && !drawable.Bitmap.IsRecycled)
			{
				// We should wrap drawable in an AsyncDrawable, nothing is deferred
				drawable = new SelfDisposingAsyncDrawable(Context.Resources, drawable.Bitmap, this);
			}
			else
			{
				// Here we asynchronously load our placeholder: it is deferred so we need a temporary AsyncDrawable
				drawable = new AsyncDrawable(Context.Resources, null, this);
				await MainThreadDispatcher.PostAsync(() =>
				{
					if (imageView == null || imageView.Handle == IntPtr.Zero)
						return;
						
					imageView.SetImageDrawable(drawable); // temporary assign this AsyncDrawable
						
				}).ConfigureAwait(false);

				try
				{
					var drawableWithResult = await RetrieveDrawableAsync(placeholderPath, source, isLoadingPlaceholder).ConfigureAwait(false);
					drawable = drawableWithResult == null ? null : drawableWithResult.Item;
				}
				catch (Exception ex)
				{
					Logger.Error("An error occured while retrieving drawable.", ex);
					return false;
				}
			}

			if (drawable == null)
				return false;

			_loadingPlaceholderWeakReference = new WeakReference<Drawable>(drawable);

			if (CancellationToken.IsCancellationRequested)
				return false;

			await MainThreadDispatcher.PostAsync(() =>
			{
				if (CancellationToken.IsCancellationRequested)
					return;
					
				if (imageView == null || imageView.Handle == IntPtr.Zero)
					return;
					
				SetImageDrawable(imageView, drawable, false);
					
			}).ConfigureAwait(false);

			return true;
		}

		/// <summary>
		/// Tries to load requested image from the cache asynchronously.
		/// </summary>
		/// <returns>A boolean indicating if image was loaded from cache.</returns>
		private async Task<CacheResult> TryLoadingFromCacheAsync(ImageView imageView)
		{
			try
			{
				if (imageView == null)
					return CacheResult.NotFound; // weird situation, dunno what to do

				if (IsCancelled)
					return CacheResult.NotFound; // not sure what to return in that case

				var key = GetKey();

				if (string.IsNullOrWhiteSpace(key))
					return CacheResult.NotFound;

				var value = ImageCache.Instance.Get(key);
				if (value == null)
					return CacheResult.NotFound; // not available in the cache

				if (IsCancelled)
					return CacheResult.NotFound; // not sure what to return in that case

				Logger.Debug(string.Format("Image from cache: {0}", key));
				await MainThreadDispatcher.PostAsync(() =>
					{
						if (IsCancelled)
							return;

						var ffDrawable = value as FFBitmapDrawable;
						if (ffDrawable != null)
							ffDrawable.StopFadeAnimation();

						if (imageView == null || imageView.Handle == IntPtr.Zero)
							return;

						imageView.SetImageDrawable(value);

						if (Utils.HasJellyBean() && imageView.AdjustViewBounds)
						{
							imageView.LayoutParameters.Height = value.IntrinsicHeight;
							imageView.LayoutParameters.Width = value.IntrinsicWidth;
						}	
					}).ConfigureAwait(false);

				if (IsCancelled)
					return CacheResult.NotFound; // not sure what to return in that case

				Completed = true;

				if (Parameters.OnSuccess != null)
					Parameters.OnSuccess(new ImageSize(value.IntrinsicWidth, value.IntrinsicHeight), LoadingResult.MemoryCache);
				return CacheResult.Found; // found and loaded from cache
			}
			catch (Exception ex)
			{
				if (Parameters.OnError != null)
					Parameters.OnError(ex);
				return CacheResult.ErrorOccured; // weird, what can we do if loading from cache fails
			}
		}

		private async Task<WithLoadingResult<Stream>> GetStreamAsync(string path, ImageSource source)
		{
			if (string.IsNullOrWhiteSpace(path)) return null;

			try
			{
				using (var resolver = StreamResolverFactory.GetResolver(source, Parameters, DownloadCache))
				{
					return await resolver.GetStream(path, CancellationToken.Token).ConfigureAwait(false);
				}
			}
			catch (System.OperationCanceledException oex)
			{
				Logger.Debug(string.Format("Image request for {0} got cancelled.", path));
				return null;
			}
			catch (Exception ex)
			{
				Logger.Error("Unable to retrieve image data", ex);
				return null;
			}
		}

		// bitmaps using the decode* methods from {@link android.graphics.BitmapFactory}. This implementation calculates
		// having a width and height equal to or larger than the requested width and height.
		private async Task<WithLoadingResult<SelfDisposingBitmapDrawable>> RetrieveDrawableAsync(string sourcePath, ImageSource source, bool isLoadingPlaceHolder)
		{
			if (string.IsNullOrWhiteSpace(sourcePath)) return null;

			// If the image cache is available and this task has not been cancelled by another
			// thread and the ImageView that was originally bound to this task is still bound back
			// to this task and our "exit early" flag is not set then try and fetch the bitmap from
			// the cache
			if (CancellationToken.IsCancellationRequested || GetAttachedImageView() == null || ImageService.ExitTasksEarly)
				return null;

			var drawableWithResult = await GetDrawableAsync(sourcePath, source, isLoadingPlaceHolder).ConfigureAwait(false);
			if (drawableWithResult == null || drawableWithResult.Item == null)
				return null;

			// FMT: even if it was canceled, if we have the bitmap we add it to the cache
			ImageCache.Instance.Add(GetKey(sourcePath), drawableWithResult.Item);
			return drawableWithResult;
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

			if (reqWidth == 0)
				reqWidth = (int)((reqHeight / height) * width);

			if (reqHeight == 0)
				reqHeight = (int)((reqWidth / width) * height);

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
			SelfDisposingBitmapDrawable bitmapDrawable = null;
			try
			{
				bitmapDrawable = ImageCache.Instance.GetBitmapDrawableFromReusableSet(options);
				var bitmap = bitmapDrawable == null ? null : bitmapDrawable.Bitmap;

				if (bitmap != null && bitmap.Handle != IntPtr.Zero)
				{
					options.InBitmap = bitmapDrawable.Bitmap;
				}
			}
			finally
			{
				if (bitmapDrawable != null)
				{
					bitmapDrawable.SetIsRetained(false);
				}
			}
		}

		private ImageView GetAttachedImageView()
		{
			ImageView imageView;
			_imageWeakReference.TryGetTarget(out imageView);

			if (imageView == null || imageView.Handle == IntPtr.Zero)
				return null;

			var task = imageView.GetImageLoaderTask();

			return this == task
				? imageView
				: null;
		}

		private void SetImageDrawable(ImageView imageView, Drawable drawable, bool fadeIn)
		{
			if (Utils.HasJellyBean() && imageView.AdjustViewBounds)
			{
				imageView.LayoutParameters.Height = drawable.IntrinsicHeight;
				imageView.LayoutParameters.Width = drawable.IntrinsicWidth;
			}

			imageView.SetImageDrawable(drawable);
		}
	}


}