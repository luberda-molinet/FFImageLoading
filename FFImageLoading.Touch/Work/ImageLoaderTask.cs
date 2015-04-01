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
		/// Once the image is processed, associates it to the imageView
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
					image = await RetrieveImageAsync().ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Logger.Error("An error occured while retrieving image.", ex);
					Parameters.OnError(ex);
					return;
				}

				if (image == null)
					return;

				var imageView = _getNativeControl();
				if (imageView == null)
					return;

				if (CancellationToken.IsCancellationRequested)
					return;

				// Post on main thread
				await MainThreadDispatcher.PostAsync(() =>
				{
					if (CancellationToken.IsCancellationRequested)
						return;

					_doWithImage(image);
					Completed = true;
					Parameters.OnSuccess();
				}).ConfigureAwait(false);
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
			return true; // found and loaded from cache
		}

		protected virtual async Task<UIImage> GetImageAsync(string sourcePath)
		{
			if (CancellationToken.IsCancellationRequested)
				return null;

			byte[] bytes = null;
			string path = sourcePath;

			try
			{
				switch (Parameters.Source)
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

				nfloat scale = _imageScale >= 1 ? _imageScale : _screenScale;
				return new UIImage(NSData.FromArray(bytes), scale);
			}).ConfigureAwait(false);
		}

		private async Task<UIImage> RetrieveImageAsync()
		{
			// If the image cache is available and this task has not been cancelled by another
			// thread and the ImageView that was originally bound to this task is still bound back
			// to this task and our "exit early" flag is not set then try and fetch the bitmap from
			// the cache
			if (CancellationToken.IsCancellationRequested || _getNativeControl() == null || ImageService.ExitTasksEarly)
				return null;

			var image = await GetImageAsync(Parameters.Path).ConfigureAwait(false);
			if (image == null)
				return null;

			// FMT: even if it was canceled, if we have the bitmap we add it to the cache
			ImageCache.Instance.Add(Parameters.Path, image);

			return image;
		}
	}
}

