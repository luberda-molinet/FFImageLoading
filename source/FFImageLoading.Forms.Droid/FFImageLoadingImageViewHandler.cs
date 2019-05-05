using System;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;

#if __ANDROID__
using Xamarin.Forms.Platform.Android;
using TNativeImageView = Android.Widget.ImageView;
#endif

//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(Xamarin.Forms.FileImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]
//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(Xamarin.Forms.StreamImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]
//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(Xamarin.Forms.UriImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]
//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(FFImageLoading.Forms.EmbeddedResourceImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]
//[assembly: Xamarin.Forms.ExportImageSourceHandler(typeof(FFImageLoading.Forms.DataUrlImageSource), typeof(FFImageLoading.Forms.Platform.FFImageLoadingImageViewHandler))]

namespace FFImageLoading.Forms.Platform
{
	[Preserve(AllMembers = true)]
	public class FFImageLoadingImageViewHandler : IImageViewHandler
	{
		public Task LoadImageAsync(Xamarin.Forms.ImageSource imagesource, TNativeImageView imageView, CancellationToken cancellationToken = default)
		{
#if __ANDROID__
			if (!IsValid(imageView))
				return Task.CompletedTask;
#endif

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
				var urlSource = (Xamarin.Forms.UriImageSource)imagesource;
				imageLoader = ImageService.Instance.LoadUrl(source.Path, urlSource.CacheValidity);

				if (!urlSource.CachingEnabled)
				{
					imageLoader.WithCache(Cache.CacheType.None);
				}
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
					.FadeAnimation(false, false)
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
#if __ANDROID__
		private static bool IsValid(TNativeImageView imageView)
		{
			if (imageView == null || imageView.Handle == IntPtr.Zero)
				return false;

			//NOTE: in some cases ContextThemeWrapper is Context
			var activity = imageView.Context as Android.App.Activity ?? (Android.App.Activity)Xamarin.Forms.Forms.Context;
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
#endif
	}
}
