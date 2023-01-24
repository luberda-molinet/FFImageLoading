using System;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;

namespace FFImageLoading.Maui.Handlers
{
	public abstract class HandlerBase<TNativeView, TImageSource> : ImageSourceService, IImageSourceService<TImageSource>
		where TNativeView: class
		where TImageSource : IImageSource
	{
		public HandlerBase(IImageService<TImageContainer> imageService)
		{
			ImageService = imageService;
		}

		protected readonly IImageService<TImageContainer> ImageService;

		protected virtual Task<IImageLoaderTask> LoadImageAsync(IImageSourceBinding binding, IImageSource imageSource, TNativeView imageView, CancellationToken cancellationToken)
		{
			TaskParameter parameters = default;

			if (binding.ImageSource == Work.ImageSource.Url)
			{
				var urlSource = (Microsoft.Maui.Controls.UriImageSource)((imageSource as IVectorImageSource)?.ImageSource ?? imageSource);
				parameters = ImageService.LoadUrl(binding.Path, urlSource.CacheValidity);

				if (!urlSource.CachingEnabled)
				{
					parameters.WithCache(Cache.CacheType.None);
				}
			}
			else if (binding.ImageSource == Work.ImageSource.CompiledResource)
			{
				parameters = ImageService.LoadCompiledResource(binding.Path);
			}
			else if (binding.ImageSource == Work.ImageSource.ApplicationBundle)
			{
				parameters = ImageService.LoadFileFromApplicationBundle(binding.Path);
			}
			else if (binding.ImageSource == Work.ImageSource.Filepath)
			{
				parameters = ImageService.LoadFile(binding.Path);
			}
			else if (binding.ImageSource == Work.ImageSource.Stream)
			{
				parameters = ImageService.LoadStream(binding.Stream);
			}
			else if (binding.ImageSource == Work.ImageSource.EmbeddedResource)
			{
				parameters = ImageService.LoadEmbeddedResource(binding.Path);
			}

			if (parameters != default)
			{
				// Enable vector image source
				if (imageSource is IVectorImageSource vect)
				{
					parameters.WithCustomDataResolver(vect.GetVectorDataResolver());
				}

				var tcs = new TaskCompletionSource<IImageLoaderTask>();

				parameters
					.FadeAnimation(false, false)
					.Error(ex => {
						tcs.TrySetException(ex);
					})
					.Finish(scheduledWork => {
						tcs.TrySetResult(scheduledWork as IImageLoaderTask);
					});

				if (cancellationToken.IsCancellationRequested)
					return Task.FromResult<IImageLoaderTask>(null);

				var task = GetImageLoaderTask(parameters, imageView);

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

			return Task.FromResult<IImageLoaderTask>(null);
		}

		protected abstract IImageLoaderTask GetImageLoaderTask(TaskParameter parameters, TNativeView imageView);
	}
}
