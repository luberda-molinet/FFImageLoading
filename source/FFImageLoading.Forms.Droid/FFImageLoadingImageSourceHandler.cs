using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using FFImageLoading.Forms.Handlers;
using FFImageLoading.Work;
using FFImageLoading.Targets;

namespace FFImageLoading.Forms.Platform
{
	public class FFImageLoadingImageSourceHandler : HandlerBase<Context>, IImageSourceHandler
	{
		public async Task<Bitmap> LoadImageAsync(Xamarin.Forms.ImageSource imageSource, Context context, CancellationToken cancellationToken = default)
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
				return target?.BitmapDrawable?.Bitmap;
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
			var activity = context as Android.App.Activity ?? (Android.App.Activity)Xamarin.Forms.Forms.Context;
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
			ImageService.Instance.LoadImage(task);
			return task;
		}
	}
}
