using System;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Maui.Handlers;
using FFImageLoading.Work;

#if __IOS__
using PImage = UIKit.UIImage;
using PImageTarget = FFImageLoading.Targets.UIImageTarget;
#elif __MACOS__
using PImage = AppKit.NSImage;
using PImageTarget = FFImageLoading.Targets.NSImageTarget;
using Xamarin.Forms.Platform.MacOS;
#endif

namespace FFImageLoading.Maui.Platform
{
	public class StreamImageSourceService : ProxyImageSourceService<IStreamImageSource>
	{
		public StreamImageSourceService(IImageService<TImageContainer> imageService)
			: base(imageService) { }
	}

	public class UriImageSourceService : ProxyImageSourceService<IUriImageSource>
	{
		public UriImageSourceService(IImageService<TImageContainer> imageService)
			: base(imageService) { }
	}

	public class FileImageSourceService : ProxyImageSourceService<IFileImageSource>
	{
		public FileImageSourceService(IImageService<TImageContainer> imageService)
			: base(imageService) { }
	}

	public class ProxyImageSourceService<TImageSource> : HandlerBase<object, TImageSource> where TImageSource : IImageSource
	{
		public ProxyImageSourceService(IImageService<TImageContainer> imageService)
			: base(imageService)
		{
			ImageService = imageService;
		}

		protected readonly IImageService<TImageContainer> ImageService;

		public override async Task<IImageSourceServiceResult<PImage>> GetImageAsync(IImageSource imageSource, float scale = 1, CancellationToken cancellationToken = default)
		{
			try
			{
				var source = ImageSourceBinding.GetImageSourceBinding(imageSource, null);
				if (source == null)
				{
					return null;
				}

				var result = await LoadImageAsync(source, imageSource, null, cancellationToken).ConfigureAwait(false);
				var target = result?.Target as PImageTarget;
				return new ImageSourceServiceResult(target?.PImage);
			}
			catch (Exception)
			{
				return null;
			}
		}

		protected override IImageLoaderTask GetImageLoaderTask(TaskParameter parameters, object imageView)
		{
			var target = new PImageTarget();
			var task = ImageService.CreateTask(parameters, target);
			ImageService.LoadImage(task);
			return task;
		}
	}
}
