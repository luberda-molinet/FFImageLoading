using System;
using System.Threading.Tasks;
using FFImageLoading.Work;
using System.IO;
using UIKit;
using FFImageLoading.Targets;
//using FFImageLoading.Extensions;
using System.Runtime.CompilerServices;

namespace FFImageLoading
{
	/// <summary>
	/// TaskParameterPlatformExtensions
	/// </summary>
	public static class TaskParameterPlatformExtensions
	{
		/// <summary>
		/// Loads the image into PNG Stream
		/// </summary>
		/// <returns>The PNG Stream async.</returns>
		/// <param name="parameters">Parameters.</param>
		public static async Task<Stream> AsPNGStreamAsync(this TaskParameter parameters, IImageService<UIImage> imageService)
		{
			var result = await AsUIImageAsync(parameters, imageService).ConfigureAwait(false);
			return result.AsPNG().AsStream();
		}

		/// <summary>
		/// Loads the image into JPG Stream
		/// </summary>
		/// <returns>The JPG Stream async.</returns>
		/// <param name="parameters">Parameters.</param>
		/// <param name="quality">Quality.</param>
		public static async Task<Stream> AsJPGStreamAsync(this TaskParameter parameters, IImageService<UIImage> imageService, int quality = 80)
		{
			var result = await AsUIImageAsync(parameters, imageService).ConfigureAwait(false);
			return result.AsJPEG(quality).AsStream();
		}

		/// <summary>
		/// Loads the image into given imageView using defined parameters.
		/// </summary>
		/// <param name="parameters">Parameters for loading the image.</param>
		/// <param name="imageView">Image view that should receive the image.</param>
		public static IScheduledWork Into(this TaskParameter parameters, UIImageView imageView, IImageService<UIImage> imageService)
		{
			var target = new UIImageViewTarget(imageService.Configuration, imageView);

			return parameters.Into(target, imageService);
		}

		/// <summary>
		/// Loads the image into given imageView using defined parameters.
		/// </summary>
		/// <param name="parameters">Parameters for loading the image.</param>
		/// <param name="item">Image view that should receive the image.</param>
		public static IScheduledWork Into(this TaskParameter parameters, UITabBarItem item, IImageService<UIImage> imageService)
		{
			var target = new UIBarItemTarget(item);
			return parameters.Into(target, imageService);
		}

		/// <summary>
		/// Loads the image into given UIButton using defined parameters.
		/// </summary>
		/// <param name="parameters">Parameters for loading the image.</param>
		/// <param name="button">UIButton that should receive the image.</param>
		public static IScheduledWork Into(this TaskParameter parameters, UIButton button, IImageService<UIImage> imageService)
		{
			var target = new UIButtonTarget(button);
			return parameters.Into(target, imageService);
		}

		/// <summary>
		/// Loads the image into given imageView using defined parameters.
		/// IMPORTANT: It throws image loading exceptions - you should handle them
		/// </summary>
		/// <returns>An awaitable Task.</returns>
		/// <param name="parameters">Parameters for loading the image.</param>
		/// <param name="imageView">Image view that should receive the image.</param>
		public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, UIImageView imageView, IImageService<UIImage> imageService)
		{
			return parameters.IntoAsync(param => param.Into(imageView, imageService));
		}

		/// <summary>
		/// Loads the image into given imageView using defined parameters.
		/// IMPORTANT: It throws image loading exceptions - you should handle them
		/// </summary>
		/// <returns>An awaitable Task.</returns>
		/// <param name="parameters">Parameters for loading the image.</param>
		/// <param name="item">Image view that should receive the image.</param>
		public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, UITabBarItem item, IImageService<UIImage> imageService)
		{
			return parameters.IntoAsync(param => param.Into(item, imageService));
		}

		/// <summary>
		/// Loads the image into given UIButton using defined parameters.
		/// IMPORTANT: It throws image loading exceptions - you should handle them
		/// </summary>
		/// <returns>An awaitable Task.</returns>
		/// <param name="parameters">Parameters for loading the image.</param>
		/// <param name="button">UIButton that should receive the image.</param>
		public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, UIButton button, IImageService<UIImage> imageService)
		{
			return parameters.IntoAsync(param => param.Into(button, imageService));
		}

		/// <summary>
		/// Loads and gets UImage using defined parameters.
		/// IMPORTANT: It throws image loading exceptions - you should handle them
		/// </summary>
		/// <returns>The PImage async.</returns>
		/// <param name="parameters">Parameters.</param>
		public static Task<UIImage> AsUIImageAsync(this TaskParameter parameters, IImageService<UIImage> imageService)
		{
			var target = new UIImageTarget();
			var userErrorCallback = parameters.OnError;
			var finishCallback = parameters.OnFinish;
			var tcs = new TaskCompletionSource<UIImage>();

			parameters
				.Error(ex =>
				{
					tcs.TrySetException(ex);
					userErrorCallback?.Invoke(ex);
				})
				.Finish(scheduledWork =>
				{
					finishCallback?.Invoke(scheduledWork);
					tcs.TrySetResult(target.PImage);
				});

			if (parameters.Source != Work.ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
			{
				target.SetAsEmpty(null);
				parameters.TryDispose();
				return null;
			}

			var task = imageService.CreateTask(parameters, target);
			imageService.LoadImage(task);

			return tcs.Task;
		}

		/// <summary>
		/// Loads the image into given target using defined parameters.
		/// </summary>
		/// <returns>The into.</returns>
		/// <param name="parameters">Parameters.</param>
		/// <param name="target">Target.</param>
		/// <typeparam name="TImageView">The 1st type parameter.</typeparam>
		public static IScheduledWork Into<TImageView>(this TaskParameter parameters, ITarget<UIImage, TImageView> target, IImageService<UIImage> imageService) where TImageView : class
		{
			if (parameters.Source != Work.ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
			{
				target.SetAsEmpty(null);
				parameters.TryDispose();
				return null;
			}

			var task = imageService.CreateTask(parameters, target);

			imageService.LoadImage(task);
			return task;
		}

		/// <summary>
		/// Loads the image into given target using defined parameters.
		/// IMPORTANT: It throws image loading exceptions - you should handle them
		/// </summary>
		/// <returns>The async.</returns>
		/// <param name="parameters">Parameters.</param>
		/// <param name="target">Target.</param>
		/// <typeparam name="TImageView">The 1st type parameter.</typeparam>
		public static Task<IScheduledWork> IntoAsync<TImageView>(this TaskParameter parameters, ITarget<UIImage, TImageView> target, ImageService imageService) where TImageView : class
		{
			return parameters.IntoAsync(param => param.Into(target, imageService));
		}

		private static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, Action<TaskParameter> into)
		{
			var userErrorCallback = parameters.OnError;
			var finishCallback = parameters.OnFinish;
			var tcs = new TaskCompletionSource<IScheduledWork>();

			parameters
				.Error(ex => {
					tcs.TrySetException(ex);
					userErrorCallback?.Invoke(ex);
				})
				.Finish(scheduledWork => {
					finishCallback?.Invoke(scheduledWork);
					tcs.TrySetResult(scheduledWork);
				});

			into(parameters);

			return tcs.Task;
		}
	}
}
