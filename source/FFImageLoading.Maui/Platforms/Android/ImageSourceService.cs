using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using FFImageLoading.Maui.Handlers;
using FFImageLoading.Work;
using FFImageLoading.Targets;
using Android.Graphics.Drawables;
using Microsoft.Maui.Platform;

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

	public abstract class ProxyImageSourceService<TImageSource> : HandlerBase<Context, TImageSource> where TImageSource : IImageSource
	{
		public ProxyImageSourceService(IImageService<TImageContainer> imageService)
			: base(imageService)
		{
			ImageService = imageService;
		}

		protected readonly IImageService<TImageContainer> ImageService;

		public override async Task<IImageSourceServiceResult<Drawable>> GetDrawableAsync(IImageSource imageSource, Context context, CancellationToken cancellationToken = default)
		{
			try
			{
				if (!IsValid(context))
					return null;

				var source = ImageSourceBinding.GetImageSourceBinding(imageSource, null);
				if (source == null)
				{
					return null;
				}

				var result = await LoadImageAsync(source, imageSource, context, cancellationToken).ConfigureAwait(false);
				var target = result.Target as BitmapTarget;
				return new ImageSourceServiceResult(target.BitmapDrawable, null);
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static bool IsValid(Context context)
		{
			if (context == null || context.Handle == IntPtr.Zero)
				return false;

#pragma warning disable CS0618 // Type or member is obsolete
			var activity = context as Android.App.Activity ?? (Android.App.Activity)Microsoft.Maui.MauiApplication.Current.GetActivity();
#pragma warning restore CS0618 // Type or member is obsolete
			if (activity != null)
			{
				if (activity.IsFinishing)
					return false;
				if (activity.IsDestroyed)
					return false;
			}
			else
			{
				return false;
			}

			return true;
		}

		protected override IImageLoaderTask GetImageLoaderTask(TaskParameter parameters, Context imageView)
		{
			var target = new BitmapTarget();
			var task = ImageService.CreateTask(parameters, target);
			ImageService.LoadImage(task);
			return task;
		}

		
	}
}
