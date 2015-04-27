using System;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using UIKit;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.IO;
using Foundation;

namespace FFImageLoading.Work
{
	public class ImageLoaderTask : ImageLoaderTaskBase
	{
		private readonly Func<UIView> _getNativeControl;
		private readonly Action<UIImage> _doWithImage;
		private readonly nfloat _imageScale;

		private static nfloat _screenScale;

		static ImageLoaderTask()
		{
			_screenScale = UIScreen.MainScreen.Scale;
		}

		public ImageLoaderTask(IDownloadCache downloadCache, IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, TaskParameter parameters, Func<UIView> getNativeControl, Action<UIImage> doWithImage, nfloat imageScale)
			: base(mainThreadDispatcher, miniLogger, parameters)
		{
			CancellationToken = new CancellationTokenSource();
			_getNativeControl = getNativeControl;
			_doWithImage = doWithImage;
			_imageScale = imageScale;
			DownloadCache = downloadCache;
		}

		protected IDownloadCache DownloadCache { get; private set; }

		/// <summary>
		/// Prepares the instance before it runs.
		/// </summary>
		public override async Task PrepareAsync()
		{
			await LoadPlaceHolderAsync(Parameters.LoadingPlaceholderPath, Parameters.LoadingPlaceholderSource).ConfigureAwait(false);
		}

		/// <summary>
		/// Runs the image loading task: gets image from file, url, asset or cache. Then assign it to the imageView.
		/// </summary>
		public override async Task RunAsync()
		{
			try
			{
				if (Completed || CancellationToken.IsCancellationRequested || ImageService.ExitTasksEarly)
					return;

				UIImage image = null;
				try
				{
					image = await RetrieveImageAsync(Parameters.Path, Parameters.Source).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Logger.Error("An error occured while retrieving image.", ex);
					Parameters.OnError(ex);
					image = null;
				}

				if (image == null)
				{
					await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
					return;
				}

				if (CancellationToken.IsCancellationRequested || _getNativeControl() == null)
					return;

				Exception trappedException = null;
				try
				{
					// Post on main thread
					await MainThreadDispatcher.PostAsync(() =>
						{
							if (CancellationToken.IsCancellationRequested)
								return;

							_doWithImage(image);
							Completed = true;
							Parameters.OnSuccess((int)image.Size.Width, (int)image.Size.Height);
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
			var nativeControl = _getNativeControl();
			if (nativeControl == null)
				return false; // weird situation, dunno what to do

			var value = ImageCache.Instance.Get(Key);
			if (value == null)
				return false; // not available in the cache

			await MainThreadDispatcher.PostAsync(() =>
				{
					_doWithImage(value);
				}).ConfigureAwait(false);

			Parameters.OnSuccess((int)value.Size.Width, (int)value.Size.Height);
			return true; // found and loaded from cache
		}

		protected virtual async Task<UIImage> GetImageAsync(string sourcePath, ImageSource source)
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

			return await Task.Run(() =>
				{
					if (CancellationToken.IsCancellationRequested)
						return null;
				
					// Special case to handle WebP decoding on iOS
					if (sourcePath.ToLowerInvariant().EndsWith(".webp", StringComparison.InvariantCulture))
					{
						return new WebP.Touch.WebPCodec().Decode(bytes);
					}

					nfloat scale = _imageScale >= 1 ? _imageScale : _screenScale;
					var image = new UIImage(NSData.FromArray(bytes), scale);

					if (Parameters.Transformations != null && Parameters.Transformations.Count > 0)
					{
						foreach (var transformation in Parameters.Transformations)
						{
							try
							{
								var bitmapHolder = transformation.Transform(new BitmapHolder(image));
								image = bitmapHolder.ToNative();
							}
							catch (Exception ex)
							{
								Logger.Error("Can't apply transformation " + transformation.Key + " to image " + path, ex);
							}
						}
					}

					return image;
				}).ConfigureAwait(false);
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

			UIImage image = null;

			try
			{
				image = await RetrieveImageAsync(placeholderPath, source).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Logger.Error("An error occured while retrieving placeholder's drawable.", ex);
				return false;
			}

			if (image == null)
				return false;
			
			var imageView = _getNativeControl();
			if (imageView == null)
				return false;

			if (CancellationToken.IsCancellationRequested)
				return false;

			// Post on main thread but don't wait for it
			MainThreadDispatcher.Post(() =>
				{
					if (CancellationToken.IsCancellationRequested)
						return;
				
					_doWithImage(image);
				});

			return true;
		}

		private async Task<UIImage> RetrieveImageAsync(string sourcePath, ImageSource source)
		{
			// If the image cache is available and this task has not been cancelled by another
			// thread and the ImageView that was originally bound to this task is still bound back
			// to this task and our "exit early" flag is not set then try and fetch the bitmap from
			// the cache
			if (CancellationToken.IsCancellationRequested || _getNativeControl() == null || ImageService.ExitTasksEarly)
				return null;

			var image = await GetImageAsync(sourcePath, source).ConfigureAwait(false);
			if (image == null)
				return null;

			// FMT: even if it was canceled, if we have the bitmap we add it to the cache
			ImageCache.Instance.Add(sourcePath + TransformationsKey, image);

			return image;
		}
	}
}

