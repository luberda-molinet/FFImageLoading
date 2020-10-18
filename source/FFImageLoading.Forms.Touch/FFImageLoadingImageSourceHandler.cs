using System;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Forms.Handlers;
using FFImageLoading.Work;

#if __IOS__
using PImage = UIKit.UIImage;
using PImageTarget = FFImageLoading.Targets.UIImageTarget;
using Xamarin.Forms.Platform.iOS;
#elif __MACOS__
using PImage = AppKit.NSImage;
using PImageTarget = FFImageLoading.Targets.NSImageTarget;
using Xamarin.Forms.Platform.MacOS;
#endif

namespace FFImageLoading.Forms.Platform
{
	public class FFImageLoadingImageSourceHandler : HandlerBase<object>, IImageSourceHandler
	{
		public async Task<PImage> LoadImageAsync(Xamarin.Forms.ImageSource imageSource, CancellationToken cancellationToken = default, float scale = 1)
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
				return target?.PImage;
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
			ImageService.Instance.LoadImage(task);
			return task;
		}
	}
}
