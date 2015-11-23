using System;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using UIKit;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.IO;
using Foundation;
using FFImageLoading.Work.DataResolver;
using System.Linq;
using System.IO;

namespace FFImageLoading.Work
{
	public class ImageLoaderTask : ImageLoaderTaskBase
	{
		private readonly Func<UIView> _getNativeControl;
		private readonly Action<UIImage, bool> _doWithImage;
		private readonly nfloat _imageScale;

		private static nfloat _screenScale;

		static ImageLoaderTask()
		{
			_screenScale = UIScreen.MainScreen.Scale;
		}

		public ImageLoaderTask(IDownloadCache downloadCache, IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, TaskParameter parameters, Func<UIView> getNativeControl, Action<UIImage, bool> doWithImage, nfloat imageScale)
			: base(mainThreadDispatcher, miniLogger, parameters)
		{
			_getNativeControl = getNativeControl;
			_doWithImage = doWithImage;
			_imageScale = imageScale;
			DownloadCache = downloadCache;
		}
			
		protected IDownloadCache DownloadCache { get; private set; }

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
			var currentControl = _getNativeControl();
			var control = task._getNativeControl();
			if (currentControl == null || control == null || currentControl.Handle == IntPtr.Zero || control.Handle == IntPtr.Zero)
				return false;

			return currentControl.Handle == control.Handle;
		}

		/// <summary>
		/// Prepares the instance before it runs.
		/// </summary>
		public override async Task<bool> PrepareAndTryLoadingFromCacheAsync()
		{
			var cacheResult = await TryLoadingFromCacheAsync().ConfigureAwait(false);
			if (cacheResult == CacheResult.Found || cacheResult == CacheResult.ErrorOccured) // If image is loaded from cache there is nothing to do here anymore, if something weird happened with the cache... error callback has already been called, let's just leave
				return true; // stop processing if loaded from cache OR if loading from cached raised an exception

			await LoadPlaceHolderAsync(Parameters.LoadingPlaceholderPath, Parameters.LoadingPlaceholderSource).ConfigureAwait(false);
			return false;
		}

		/// <summary>
		/// Runs the image loading task: gets image from file, url, asset or cache. Then assign it to the imageView.
		/// </summary>
		protected override async Task<GenerateResult> TryGeneratingImageAsync()
		{
			WithLoadingResult<UIImage> imageWithResult = null;
			UIImage image = null;
			try
			{
				imageWithResult = await RetrieveImageAsync(Parameters.Path, Parameters.Source).ConfigureAwait(false);
				image = imageWithResult == null ? null : imageWithResult.Item;
			}
			catch (Exception ex)
			{
				Logger.Error("An error occured while retrieving image.", ex);
				image = null;
			}

			if (image == null)
			{
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
				return GenerateResult.Failed;
			}

			if (CancellationToken.IsCancellationRequested)
				return GenerateResult.Canceled;
					
			if (_getNativeControl() == null)
				return GenerateResult.InvalidTarget;

			Exception trappedException = null;
			try
			{
				// Post on main thread
				await MainThreadDispatcher.PostAsync(() =>
					{
						if (CancellationToken.IsCancellationRequested)
							return;

						_doWithImage(image, false);
						Completed = true;
						Parameters.OnSuccess(new ImageSize((int)image.Size.Width, (int)image.Size.Height), imageWithResult.Result);
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
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
				throw trappedException;
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
				var nativeControl = _getNativeControl();
				if (nativeControl == null)
					return CacheResult.NotFound; // weird situation, dunno what to do

	            var value = ImageCache.Instance.Get(GetKey());
				if (value == null)
					return CacheResult.NotFound; // not available in the cache

				if (IsCancelled)
					return CacheResult.NotFound; // not sure what to return in that case

				await MainThreadDispatcher.PostAsync(() =>
					{
						_doWithImage(value, true);
					}).ConfigureAwait(false);

				if (IsCancelled)
					return CacheResult.NotFound; // not sure what to return in that case

				Completed = true;

				if (Parameters.OnSuccess != null)
					Parameters.OnSuccess(new ImageSize((int)value.Size.Width, (int)value.Size.Height), LoadingResult.MemoryCache);
				
				return CacheResult.Found; // found and loaded from cache
			}
			catch (Exception ex)
			{
				Parameters.OnError(ex);
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

			if (CancellationToken.IsCancellationRequested)
				return GenerateResult.Canceled;

			WithLoadingResult<UIImage> imageWithResult = null;
			UIImage image = null;
			try
			{
				imageWithResult = await GetImageAsync("Stream", ImageSource.Stream, stream).ConfigureAwait(false);
				image = imageWithResult == null ? null : imageWithResult.Item;
			}
			catch (Exception ex)
			{
				Logger.Error("An error occured while retrieving image.", ex);
				image = null;
			}

			if (image == null)
			{
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
				return GenerateResult.Failed;
			}

			if (CancellationToken.IsCancellationRequested)
				return GenerateResult.Canceled;

			if (_getNativeControl() == null)
				return GenerateResult.InvalidTarget;

			Exception trappedException = null;
			try
			{
				// Post on main thread
				await MainThreadDispatcher.PostAsync(() =>
					{
						if (CancellationToken.IsCancellationRequested)
							return;

						_doWithImage(image, false);
						Completed = true;
						Parameters.OnSuccess(new ImageSize((int)image.Size.Width, (int)image.Size.Height), imageWithResult.Result);
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
				await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
				throw trappedException;
			}

			return GenerateResult.Success;
		}

		protected virtual async Task<WithLoadingResult<UIImage>> GetImageAsync(string sourcePath, ImageSource source, Stream originalStream = null)
		{
			if (CancellationToken.IsCancellationRequested)
				return null;

			byte[] bytes = null;
			string path = sourcePath;
			LoadingResult? result = null;

			try
			{
				if (originalStream != null)
				{
					using (var ms = new MemoryStream())
					{
						await originalStream.CopyToAsync(ms).ConfigureAwait(false);
						bytes = ms.ToArray();
						path = sourcePath;
						result = LoadingResult.Stream;
					}
				}
				else
				{
					using (var resolver = DataResolverFactory.GetResolver(source, Parameters, DownloadCache))
					{
						var data = await resolver.GetData(path, CancellationToken.Token).ConfigureAwait(false);
						if (data == null)
							return null;

						bytes = data.Data;
						path = data.ResultIdentifier;
						result = data.Result;
					}
				}
			}
			catch (System.OperationCanceledException oex)
			{
				Logger.Debug(string.Format("Image request for {0} got cancelled.", path));
				return null;
			}
			catch (Exception ex)
			{
				var message = String.Format("Unable to retrieve image data from source: {0}", sourcePath);
				Logger.Error(message, ex);
				Parameters.OnError(ex);
				return null;
			}

			if (bytes == null)
				return null;

			var image = await Task.Run(() =>
				{
					if (CancellationToken.IsCancellationRequested)
						return null;
				
					// Special case to handle WebP decoding on iOS
					if (sourcePath.ToLowerInvariant().EndsWith(".webp", StringComparison.InvariantCulture))
					{
						return new WebP.Touch.WebPCodec().Decode(bytes);
					}

					nfloat scale = _imageScale >= 1 ? _imageScale : _screenScale;
					var imageIn = new UIImage(NSData.FromArray(bytes), scale);

					if (Parameters.Transformations != null && Parameters.Transformations.Count > 0)
					{
						foreach (var transformation in Parameters.Transformations.ToList() /* to prevent concurrency issues */)
						{
							if (CancellationToken.IsCancellationRequested)
								return null;

							try
							{
								var old = imageIn;
								var bitmapHolder = transformation.Transform(new BitmapHolder(imageIn));
								imageIn = bitmapHolder.ToNative();

								// Transformation succeeded, so garbage the source
								old.Dispose();
							}
							catch (Exception ex)
							{
								Logger.Error("Can't apply transformation " + transformation.Key + " to image " + path, ex);
							}
						}
					}

					return imageIn;
				}).ConfigureAwait(false);

			return WithLoadingResult.Encapsulate(image, result.Value);
		}

		/// <summary>
		/// Loads given placeHolder into the imageView.
		/// </summary>
		/// <returns>An awaitable task.</returns>
		/// <param name="placeholderPath">Full path to the placeholder.</param>
		/// <param name="source">Source for the path: local, web, assets</param>
		private async Task<bool> LoadPlaceHolderAsync(string placeholderPath, ImageSource source)
		{
			if (string.IsNullOrWhiteSpace(placeholderPath))
				return false;

			UIImage image = ImageCache.Instance.Get(GetKey(placeholderPath));
			if (image == null)
			{
				try
				{
					var imageWithResult = await RetrieveImageAsync(placeholderPath, source).ConfigureAwait(false);
					image = imageWithResult == null ? null : imageWithResult.Item;
				}
				catch (Exception ex)
				{
					Logger.Error("An error occured while retrieving placeholder's drawable.", ex);
					return false;
				}
			}

			if (image == null)
				return false;
			
			var view = _getNativeControl();
			if (view == null)
				return false;

			if (CancellationToken.IsCancellationRequested)
				return false;

			// Post on main thread but don't wait for it
			MainThreadDispatcher.Post(() =>
				{
					if (CancellationToken.IsCancellationRequested)
						return;
				
					_doWithImage(image, false);
				});

			return true;
		}

		private async Task<WithLoadingResult<UIImage>> RetrieveImageAsync(string sourcePath, ImageSource source)
		{
			// If the image cache is available and this task has not been cancelled by another
			// thread and the ImageView that was originally bound to this task is still bound back
			// to this task and our "exit early" flag is not set then try and fetch the bitmap from
			// the cache
			if (CancellationToken.IsCancellationRequested || _getNativeControl() == null || ImageService.ExitTasksEarly)
				return null;

			var imageWithResult = await GetImageAsync(sourcePath, source).ConfigureAwait(false);
			if (imageWithResult == null || imageWithResult.Item == null)
				return null;

			// FMT: even if it was canceled, if we have the bitmap we add it to the cache
			ImageCache.Instance.Add(GetKey(sourcePath), imageWithResult.Item);

			return imageWithResult;
		}
	}
}

