using System;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;

#if __ANDROID__
using Xamarin.Forms.Platform.Android;
using TNativeImageView = Android.Widget.ImageView;
#endif

namespace FFImageLoading.Forms.Platform
{
	public class FFImageLoadingImageViewHandler : IImageViewHandler
	{
		public Task LoadImageAsync(Xamarin.Forms.ImageSource imagesource, TNativeImageView imageView, CancellationToken cancellationToken = default)
		{
			var source = ImageSourceBinding.GetImageSourceBinding(imagesource, null);
			if (source == null)
			{
#if __ANDROID__
				imageView.SetImageResource(Android.Resource.Color.Transparent);
#endif
				return Task.CompletedTask;
			}

			TaskParameter imageLoader;

			if (source.ImageSource == ImageSource.Url)
			{
				imageLoader = ImageService.Instance.LoadUrl(source.Path);
			}
			else if (source.ImageSource == ImageSource.CompiledResource)
			{
				imageLoader = ImageService.Instance.LoadCompiledResource(source.Path);
			}
			else if (source.ImageSource == ImageSource.ApplicationBundle)
			{
				imageLoader = ImageService.Instance.LoadFileFromApplicationBundle(source.Path);
			}
			else if (source.ImageSource == ImageSource.Filepath)
			{
				imageLoader = ImageService.Instance.LoadFile(source.Path);
			}
			else if (source.ImageSource == ImageSource.Stream)
			{
				imageLoader = ImageService.Instance.LoadStream(source.Stream);
			}
			else if (source.ImageSource == ImageSource.EmbeddedResource)
			{
				imageLoader = ImageService.Instance.LoadEmbeddedResource(source.Path);
			}
			else
			{
				return Task.CompletedTask;
			}

			if (imageLoader != null)
			{
				var tcs = new TaskCompletionSource<IScheduledWork>();

				imageLoader
					.Error(ex => {
						tcs.TrySetException(ex);
					})
					.Finish(scheduledWork => {
						tcs.TrySetResult(scheduledWork);
					});

				var task = imageLoader.Into(imageView);

				if (cancellationToken != default)
					cancellationToken.Register(() =>
					{
						try
						{
							task?.Cancel();
						}
						catch { }
					});

				return tcs.Task;
			}

			return Task.CompletedTask;
		}
	}
}
