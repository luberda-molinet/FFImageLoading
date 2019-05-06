using System;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;
using Android.Widget;
using FFImageLoading.Forms.Handlers;
using Xamarin.Forms.Platform.Android;

//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(Xamarin.Forms.FileImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]
//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(Xamarin.Forms.StreamImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]
//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(Xamarin.Forms.UriImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]
//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(FFImageLoading.Forms.EmbeddedResourceImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]
//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(FFImageLoading.Forms.DataUrlImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]

namespace FFImageLoading.Forms.Platform
{
	[Preserve(AllMembers = true)]
	public class FFImageLoadingImageViewHandler : HandlerBase<ImageView>, IImageViewHandler
	{
		public Task LoadImageAsync(Xamarin.Forms.ImageSource imageSource, ImageView imageView, CancellationToken cancellationToken = default)
		{
			try
			{
				if (!IsValid(imageView))
					return Task.CompletedTask;

				var source = ImageSourceBinding.GetImageSourceBinding(imageSource, null);
				if (source == null)
				{
					imageView.SetImageResource(Android.Resource.Color.Transparent);
					return Task.CompletedTask;
				}

				return LoadImageAsync(source, imageSource, imageView, cancellationToken);
			}
			catch (Exception)
			{
				return Task.CompletedTask;
			}
		}

		private static bool IsValid(ImageView imageView)
		{
			if (imageView == null || imageView.Handle == IntPtr.Zero)
				return false;
				
#pragma warning disable CS0618 // Type or member is obsolete
			var activity = imageView.Context as Android.App.Activity ?? (Android.App.Activity)Xamarin.Forms.Forms.Context;
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

		protected override IImageLoaderTask GetImageLoaderTask(TaskParameter parameters, ImageView imageView)
		{
			return parameters.Into(imageView) as IImageLoaderTask;
		}
	}
}
