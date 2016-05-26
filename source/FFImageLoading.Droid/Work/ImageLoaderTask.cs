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
		private static readonly SemaphoreSlim _decodingLock = new SemaphoreSlim(1);

		private WeakReference<BitmapDrawable> _loadingPlaceholderWeakReference;
		internal ITarget<BitmapDrawable, ImageLoaderTask> _target;

		public ImageLoaderTask(IDownloadCache downloadCache, IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, TaskParameter parameters, ITarget<BitmapDrawable, ImageLoaderTask> target)
			: base(mainThreadDispatcher, miniLogger, parameters, true)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			_target = target;
			DownloadCache = downloadCache;
		}

		/// <summary>
		/// This constructor is useful for child classes only. It can help when having a totally different loading logic.
		/// </summary>
		/// <param name="miniLogger">Logger</param>
		/// <param name="key">Key.</param>
		/// <param name="imageView">Image view.</param>
		protected ImageLoaderTask(IDownloadCache downloadCache, IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, string key, ITarget<BitmapDrawable, ImageLoaderTask> target)
			: this(downloadCache, mainThreadDispatcher, miniLogger, TaskParameter.FromFile(key), target)
		{
		}

		/// <summary>
		/// Indicates if the task uses the same native control
		/// </summary>
		/// <returns><c>true</c>, if same native control is used, <c>false</c> otherwise.</returns>
		/// <param name="task">Task to check.</param>
		public override bool UsesSameNativeControl(IImageLoaderTask task)
		{
			return _target.UsesSameNativeControl((ImageLoaderTask)task);
		}

		/// <summary>
		/// Prepares the instance before it runs.
		/// </summary>
		public override async Task<bool> PrepareAndTryLoadingFromCacheAsync()
		{
			if (!_target.IsValid)
				return false;

			if (CanUseMemoryCache())
			{
				var cacheResult = await TryLoadingFromCacheAsync().ConfigureAwait(false);
				if (cacheResult == CacheResult.Found || cacheResult == CacheResult.ErrorOccured) // If image is loaded from cache there is nothing to do here anymore, if something weird happened with the cache... error callback has already been called, let's just leave
				return true; // stop processing if loaded from cache OR if loading from cached raised an exception

				if (IsCancelled)
					return true; // stop processing if cancelled
			}

			bool hasDrawable = await LoadPlaceHolderAsync(Parameters.LoadingPlaceholderPath, Parameters.LoadingPlaceholderSource, true).ConfigureAwait(false);
			if (!hasDrawable)
			{
				// Assign the Drawable to the image
				var drawable = new AsyncDrawable(Context.Resources, null, this);
				await MainThreadDispatcher.PostAsync(() => _target.Set(this, drawable, true, true)).ConfigureAwait(false);
			}

			return false;
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
			WithLoadingResult<SelfDisposingBitmapDrawable> drawableWithResult;
			if (string.IsNullOrWhiteSpace(Parameters.Path))
			{
				drawableWithResult = new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Failed);
			}
			else
			{
				try
				{
					drawableWithResult = await RetrieveDrawableAsync(Parameters.Path, Parameters.Source, false, false).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Logger.Error("An error occured while retrieving drawable.", ex);
					drawableWithResult = new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Failed);
				}
			}

			if (!_target.IsTaskValid(this))
				return GenerateResult.InvalidTarget;

			if (drawableWithResult.HasError)
			{
				// Show error placeholder
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource, false).ConfigureAwait(false);
				return drawableWithResult.GenerateResult;
			}
				
			try
			{
				if (IsCancelled)
					return GenerateResult.Canceled;

				// Post on main thread
				await MainThreadDispatcher.PostAsync(() =>
					{
						_target.Set(this, drawableWithResult.Item, drawableWithResult.Result.IsLocalOrCachedResult(), false);
						Completed = true;
						Parameters?.OnSuccess(drawableWithResult.ImageInformation, drawableWithResult.Result);
					}).ConfigureAwait(false);

				if (!Completed)
					return GenerateResult.Failed;
			}
			catch (Exception ex2)
			{
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource, false).ConfigureAwait(false);
				throw ex2;
			}

			return GenerateResult.Success;
		}

		/// <summary>
		/// Tries to load requested image from the cache asynchronously.
		/// </summary>
		/// <returns>A boolean indicating if image was loaded from cache.</returns>
		public override async Task<CacheResult> TryLoadingFromCacheAsync()
		{
			try
			{
				if (!_target.IsValid)
					return CacheResult.NotFound; // weird situation, dunno what to do

				if (IsCancelled)
					return CacheResult.NotFound; // not sure what to return in that case

				var key = GetKey();

				if (string.IsNullOrWhiteSpace(key))
					return CacheResult.NotFound;

				var cacheEntry = ImageCache.Instance.Get(key);

				if (cacheEntry == null)
					return CacheResult.NotFound; // not available in the cache

				var value = cacheEntry.Item1;

				if (value == null)
					return CacheResult.NotFound; // not available in the cache

				if (IsCancelled)
					return CacheResult.NotFound; // not sure what to return in that case

				value.SetIsRetained(true);

				try
				{
					Logger.Debug(string.Format("Image from cache: {0}", key));
					await MainThreadDispatcher.PostAsync(() =>
						{
							if (IsCancelled)
								return;

							var ffDrawable = value as FFBitmapDrawable;
							if (ffDrawable != null)
								ffDrawable.StopFadeAnimation();

							_target.Set(this, value, true, false);

							Completed = true;

							Parameters?.OnSuccess(cacheEntry.Item2, LoadingResult.MemoryCache);
						}).ConfigureAwait(false);

					if (!Completed)
						return CacheResult.NotFound; // not sure what to return in that case

					return CacheResult.Found; // found and loaded from cache
				}
				finally
				{
					value.SetIsRetained(false);
				}
			}
			catch (Exception ex)
			{
				Parameters?.OnError(ex);
				return CacheResult.ErrorOccured; // weird, what can we do if loading from cache fails
			}
		}

		/// <summary>
		/// Loads the image from given stream asynchronously.
		/// </summary>
		/// <returns>An awaitable task.</returns>
		/// <param name="stream">The stream to get data from.</param>
		public override async Task<GenerateResult> LoadFromStreamAsync(Stream stream)
		{
			if (stream == null)
				return GenerateResult.Failed;

			if (IsCancelled)
				return GenerateResult.Canceled;

			if (!_target.IsTaskValid(this))
				return GenerateResult.InvalidTarget;

			var resultWithDrawable = await GetDrawableAsync("Stream", ImageSource.Stream, false, false, stream).ConfigureAwait(false);
			if (resultWithDrawable.HasError)
			{
				// Show error placeholder
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource, false).ConfigureAwait(false);

				return resultWithDrawable.GenerateResult;
			}

			if (CanUseMemoryCache())
			{
				ImageCache.Instance.Add(GetKey(), resultWithDrawable.ImageInformation, resultWithDrawable.Item);
			}

			try
			{
				if (IsCancelled)
					return GenerateResult.Canceled;

				// Post on main thread
				await MainThreadDispatcher.PostAsync(() =>
					{
						_target.Set(this, resultWithDrawable.Item, true, false);
						
						Completed = true;
						Parameters?.OnSuccess(resultWithDrawable.ImageInformation, resultWithDrawable.Result);
					}).ConfigureAwait(false);

				if (!Completed)
					return GenerateResult.Failed;
			}
			catch (Exception ex2)
			{
				// Show error placeholder
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource, false).ConfigureAwait(false);
				throw ex2;
			}

			return GenerateResult.Success;
		}

		protected virtual async Task<WithLoadingResult<SelfDisposingBitmapDrawable>> GetDrawableAsync(string path, ImageSource source, bool isLoadingPlaceHolder, bool isPlaceholder, Stream originalStream = null)
		{
			if (IsCancelled)
				return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Canceled);

			// First decode with inJustDecodeBounds=true to check dimensions
			var options = new BitmapFactory.Options
			{
				InJustDecodeBounds = true
			};

			Stream stream = null;
			WithLoadingResult<Stream> streamWithResult;
			if (originalStream != null)
			{
				streamWithResult = new WithLoadingResult<Stream>(originalStream, LoadingResult.Stream);
			}
			else
			{
				streamWithResult = await GetStreamAsync(path, source).ConfigureAwait(false);
			}

			if (streamWithResult.HasError)
			{
				if (streamWithResult.Result == LoadingResult.NotFound)
				{
					Logger.Error(string.Format("Not found: {0} from {1}", path, source.ToString()));
				}
				return new WithLoadingResult<SelfDisposingBitmapDrawable>(streamWithResult.Result);
			}

			stream = streamWithResult.Item;

			try
			{
				try
				{
					BitmapFactory.DecodeStream(stream, null, options);

					if (!stream.CanSeek)
					{
						if (stream == originalStream)
						{
							// If we cannot seek the original stream then there's not much we can do
							return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Failed);
						}
						else
						{
							// Assets stream can't be seeked to origin position
							stream.Dispose();
							streamWithResult = await GetStreamAsync(path, source).ConfigureAwait(false);
							if (streamWithResult.HasError)
							{
								return new WithLoadingResult<SelfDisposingBitmapDrawable>(streamWithResult.Result);
							}

							stream = streamWithResult.Item;
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
					return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Failed);
				}

				if (IsCancelled)
					return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Canceled);

				// Setting image informations
				var imageInformation = streamWithResult.ImageInformation ?? new ImageInformation();
				imageInformation.SetOriginalSize(options.OutWidth, options.OutHeight);
				imageInformation.SetCurrentSize(options.OutWidth, options.OutHeight);
				imageInformation.SetKey(path == "Stream" ? GetKey() : GetKey(path), Parameters.CustomCacheKey);

				options.InPurgeable = true;
				options.InJustDecodeBounds = false;

				if (!ImageService.Instance.Config.LoadWithTransparencyChannel || Parameters.LoadTransparencyChannel == null || !Parameters.LoadTransparencyChannel.Value)
				{
					// Same quality but no transparency channel. This allows to save 50% of memory: 1 pixel=2bytes instead of 4.
					options.InPreferredConfig = Bitmap.Config.Rgb565;
				}

				// CHECK IF BITMAP IS EXIF ROTATED
				int exifRotation = 0;
				if (source == ImageSource.Filepath)
				{
					exifRotation = path.GetExifRotationDegrees();
				}

				try
				{
					if (Parameters.DownSampleSize != null && (Parameters.DownSampleSize.Item1 > 0 || Parameters.DownSampleSize.Item2 > 0))
					{
						// Calculate inSampleSize
						int downsampleWidth = Parameters.DownSampleSize.Item1;
						int downsampleHeight = Parameters.DownSampleSize.Item2;

						// if image is rotated, swap width/height
						if (exifRotation == 90 || exifRotation == 270) 
						{
							downsampleWidth = Parameters.DownSampleSize.Item2;
							downsampleHeight = Parameters.DownSampleSize.Item1;
						}

						if (Parameters.DownSampleUseDipUnits)
						{
							downsampleWidth = downsampleWidth.DpToPixels();
							downsampleHeight = downsampleHeight.DpToPixels();
						}

						options.InSampleSize = CalculateInSampleSize(options, downsampleWidth, downsampleHeight);

						if (options.InSampleSize > 1)
							imageInformation.SetCurrentSize(
								(int)((double)options.OutWidth / options.InSampleSize), 
								(int)((double)options.OutHeight / options.InSampleSize));

						// If we're running on Honeycomb or newer, try to use inBitmap
						if (Utils.HasHoneycomb())
							AddInBitmapOptions(options);	
					}
				}
				catch (Exception ex)
				{
					Logger.Error("Something wrong happened while adding decoding options to image: " + path, ex);
				}

				if (IsCancelled)
					return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Canceled);

				Bitmap bitmap;
				try
				{
					bitmap = BitmapFactory.DecodeStream(stream, null, options);
				}
				catch (Java.Lang.Throwable vme)
				{
					if (vme.Class == Java.Lang.Class.FromType(typeof(Java.Lang.OutOfMemoryError)))
					{
						ImageCache.Instance.Clear(); // Clear will also force a Garbage collection
					}
					return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Failed);
				}
				catch (Exception ex)
				{
					Logger.Error("Something wrong happened while asynchronously loading/decoding image: " + path, ex);
					return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Failed);
				}

				if (bitmap == null)
					return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Failed);

				if (IsCancelled)
					return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Canceled);

				// APPLY EXIF ORIENTATION IF NEEDED
				if (exifRotation != 0)
					bitmap = bitmap.ToRotatedBitmap(exifRotation);

				bool transformPlaceholdersEnabled = Parameters.TransformPlaceholdersEnabled.HasValue ? 
					Parameters.TransformPlaceholdersEnabled.Value : ImageService.Instance.Config.TransformPlaceholders;

				if (Parameters.Transformations != null && Parameters.Transformations.Count > 0
					&& (!isPlaceholder || (isPlaceholder && transformPlaceholdersEnabled)))
				{
                    await _decodingLock.WaitAsync().ConfigureAwait(false); // Applying transformations is both CPU and memory intensive
                    try
                    {
                        foreach (var transformation in Parameters.Transformations.ToList() /* to prevent concurrency issues */)
                        {
                            if (IsCancelled)
                                return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Canceled);

                            try
                            {
                                var old = bitmap;
                                var bitmapHolder = transformation.Transform(new BitmapHolder(bitmap));
                                bitmap = bitmapHolder.ToNative();

                                // Transformation succeeded, so garbage the source
                                if (old != null && old.Handle != IntPtr.Zero && !old.IsRecycled && old != bitmap && old.Handle != bitmap.Handle)
                                {
                                    old.Recycle();
                                    old.Dispose();
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Can't apply transformation " + transformation.Key + " to image " + path, ex);
                            }
                        }
                    }
                    finally
                    {
                        _decodingLock.Release();
                    }
				}

				if (isLoadingPlaceHolder)
				{
					return WithLoadingResult.Encapsulate<SelfDisposingBitmapDrawable>(
						new SelfDisposingAsyncDrawable(Context.Resources, bitmap, this), streamWithResult.Result, imageInformation);
				}
				else
				{
					bool isFadeAnimationEnabled = Parameters.FadeAnimationEnabled.HasValue ?
						Parameters.FadeAnimationEnabled.Value : ImageService.Instance.Config.FadeAnimationEnabled;

					bool isFadeAnimationEnabledForCached = isFadeAnimationEnabled && (Parameters.FadeAnimationForCachedImages.HasValue ?
						Parameters.FadeAnimationForCachedImages.Value : ImageService.Instance.Config.FadeAnimationForCachedImages);

					int fadeDuration = Parameters.FadeAnimationDuration.HasValue ?
						Parameters.FadeAnimationDuration.Value : ImageService.Instance.Config.FadeAnimationDuration;

					bool isLocalOrCached = streamWithResult.Result.IsLocalOrCachedResult();

					BitmapDrawable placeholderDrawable = null;
					if (_loadingPlaceholderWeakReference != null)
					{
						_loadingPlaceholderWeakReference.TryGetTarget(out placeholderDrawable);
					}

					if (isLocalOrCached)
					{
						return WithLoadingResult.Encapsulate<SelfDisposingBitmapDrawable>(
							new FFBitmapDrawable(Context.Resources, bitmap, placeholderDrawable, 
								fadeDuration, isFadeAnimationEnabled && isFadeAnimationEnabledForCached), 
								streamWithResult.Result, imageInformation);
					}

					return WithLoadingResult.Encapsulate<SelfDisposingBitmapDrawable>(
						new FFBitmapDrawable(Context.Resources, bitmap, placeholderDrawable, 
							fadeDuration, isFadeAnimationEnabled), 
							streamWithResult.Result, imageInformation);
				}
			}
			finally
			{
				if (stream != null)
					stream.Dispose();
			}
		}

		/// <summary>
		/// Loads given placeHolder into the imageView.
		/// </summary>
		/// <returns>An awaitable task.</returns>
		/// <param name="placeholderPath">Full path to the placeholder.</param>
		/// <param name="source">Source for the path: local, web, assets</param>
		protected async Task<bool> LoadPlaceHolderAsync(string placeholderPath, ImageSource source, bool isLoadingPlaceholder)
		{
			if (string.IsNullOrWhiteSpace(placeholderPath))
				return false;

			if (!_target.IsValid)
				return false;

			bool isLocalOrFromCache = true;
			var cacheEntry = ImageCache.Instance.Get(GetKey(placeholderPath));

			BitmapDrawable drawable = cacheEntry == null ? null: cacheEntry.Item1;

			if (drawable != null && drawable.Handle != IntPtr.Zero 
				&& drawable.Bitmap != null && drawable.Bitmap.Handle != IntPtr.Zero && !drawable.Bitmap.IsRecycled)
			{
				// We should wrap drawable in an AsyncDrawable, nothing is deferred
				drawable = new SelfDisposingAsyncDrawable(Context.Resources, drawable.Bitmap, this);

				await MainThreadDispatcher.PostAsync(() => _target.Set(this, drawable, isLocalOrFromCache, isLoadingPlaceholder)).ConfigureAwait(false);
			}
			else
			{
				// Here we asynchronously load our placeholder: it is deferred so we need a temporary AsyncDrawable
				drawable = new AsyncDrawable(Context.Resources, null, this);
				await MainThreadDispatcher.PostAsync(() => _target.Set(this, drawable, true, isLoadingPlaceholder)).ConfigureAwait(false); // temporary assign this AsyncDrawable

				try
				{
					var drawableWithResult = await RetrieveDrawableAsync(placeholderPath, source, true, true).ConfigureAwait(false);
					drawable = drawableWithResult.Item;
					isLocalOrFromCache = drawableWithResult.Result.IsLocalOrCachedResult();
				}
				catch (Exception ex)
				{
					Logger.Error("An error occured while retrieving drawable.", ex);
					return false;
				}
			}

			if (drawable == null)
				return false;

			if (isLoadingPlaceholder)
				_loadingPlaceholderWeakReference = new WeakReference<BitmapDrawable>(drawable);

			return true;
		}

		private async Task<WithLoadingResult<Stream>> GetStreamAsync(string path, ImageSource source)
		{
			if (string.IsNullOrWhiteSpace(path))
				return new WithLoadingResult<Stream>(LoadingResult.Failed);

			try
			{
				using (var resolver = StreamResolverFactory.GetResolver(source, Parameters, DownloadCache))
				{
					return await resolver.GetStream(path, CancellationToken.Token).ConfigureAwait(false);
				}
			}
			catch (System.OperationCanceledException)
			{
				Logger.Debug(string.Format("Image request for {0} got cancelled.", path));
				return new WithLoadingResult<Stream>(LoadingResult.Canceled);
			}
			catch (Exception ex)
			{
				Logger.Error("Unable to retrieve image data", ex);
				return new WithLoadingResult<Stream>(LoadingResult.Failed);
			}
		}

		// bitmaps using the decode* methods from {@link android.graphics.BitmapFactory}. This implementation calculates
		// having a width and height equal to or larger than the requested width and height.
		private async Task<WithLoadingResult<SelfDisposingBitmapDrawable>> RetrieveDrawableAsync(string sourcePath, ImageSource source, bool isLoadingPlaceHolder, bool isPlaceholder)
		{
			if (string.IsNullOrWhiteSpace(sourcePath))
				return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Failed);

			// If the image cache is available and this task has not been cancelled by another
			// thread and the ImageView that was originally bound to this task is still bound back
			// to this task and our "exit early" flag is not set then try and fetch the bitmap from
			// the cache
			if (IsCancelled || ImageService.Instance.ExitTasksEarly)
				return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.Canceled);

			if (!_target.IsTaskValid(this))
				return new WithLoadingResult<SelfDisposingBitmapDrawable>(LoadingResult.InvalidTarget);

			var resultWithDrawable = await GetDrawableAsync(sourcePath, source, isLoadingPlaceHolder, isPlaceholder).ConfigureAwait(false);
			if (resultWithDrawable.HasError)
				return resultWithDrawable;

			// FMT: even if it was canceled, if we have the bitmap we add it to the cache
			ImageCache.Instance.Add(GetKey(sourcePath), resultWithDrawable.ImageInformation, resultWithDrawable.Item);
			return resultWithDrawable;
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

				if (bitmap != null && bitmap.Handle != IntPtr.Zero && !bitmap.IsRecycled)
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
	}
}