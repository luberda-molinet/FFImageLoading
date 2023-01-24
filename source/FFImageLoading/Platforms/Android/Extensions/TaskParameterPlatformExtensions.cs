using System;
using System.Threading.Tasks;
using FFImageLoading.Work;
using System.IO;
using FFImageLoading.Targets;
using FFImageLoading.Drawables;
using Android.Widget;
using FFImageLoading.Helpers;

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
		public static async Task<Stream> AsPNGStreamAsync(this TaskParameter parameters, IImageService<SelfDisposingBitmapDrawable> imageService)
		{
			using (var result = await AsBitmapDrawableAsync(parameters, imageService).ConfigureAwait(false))
			{
				var stream = await result.AsPngStreamAsync().ConfigureAwait(false);

				return stream;
			}
		}

		/// <summary>
		/// Loads the image into JPG Stream
		/// </summary>
		/// <returns>The JPG Stream async.</returns>
		/// <param name="parameters">Parameters.</param>
		/// <param name="quality">Quality.</param>
		public static async Task<Stream> AsJPGStreamAsync(this TaskParameter parameters, IImageService<SelfDisposingBitmapDrawable> imageService, int quality = 80)
		{
			using (var result = await AsBitmapDrawableAsync(parameters, imageService).ConfigureAwait(false))
			{
				var stream = await result.AsJpegStreamAsync(quality).ConfigureAwait(false);
				result.SetIsDisplayed(false);

				return stream;
			}
		}

		/// <summary>
		/// Loads and gets BitmapDrawable using defined parameters.
		/// IMPORTANT: you should call SetNoLongerDisplayed method if drawable is no longer displayed
		/// IMPORTANT: It throws image loading exceptions - you should handle them
		/// </summary>
		/// <returns>The bitmap drawable async.</returns>
		/// <param name="parameters">Parameters.</param>
		public static Task<SelfDisposingBitmapDrawable> AsBitmapDrawableAsync(this TaskParameter parameters, IImageService<SelfDisposingBitmapDrawable> imageService)
		{
			var target = new BitmapTarget();
			var userErrorCallback = parameters.OnError;
			var finishCallback = parameters.OnFinish;
			var tcs = new TaskCompletionSource<SelfDisposingBitmapDrawable>();

			parameters
				.Error(ex =>
				{
					tcs.TrySetException(ex);
					userErrorCallback?.Invoke(ex);
				})
				.Finish(scheduledWork =>
				{
					finishCallback?.Invoke(scheduledWork);
					tcs.TrySetResult(target.BitmapDrawable);
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
		/// Loads the image into given ImageView using defined parameters.
		/// </summary>
		/// <param name="parameters">Parameters for loading the image.</param>
		/// <param name="imageView">Image view that should receive the image.</param>
		public static IScheduledWork Into(this TaskParameter parameters, ImageView imageView, IImageService<SelfDisposingBitmapDrawable> imageService)
		{
			var target = new ImageViewTarget(imageView, imageService.Logger);
			return parameters.Into(target, imageService);
		}

		/// <summary>
		/// Loads the image into given ImageView using defined parameters.
		/// IMPORTANT: It throws image loading exceptions - you should handle them
		/// </summary>
		/// <returns>An awaitable Task.</returns>
		/// <param name="parameters">Parameters for loading the image.</param>
		/// <param name="imageView">Image view that should receive the image.</param>
		public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, ImageView imageView, IImageService<SelfDisposingBitmapDrawable> imageService)
		{
			return parameters.IntoAsync(param => param.Into(imageView, imageService));
		}

		/// <summary>
		/// Loads the image into given target using defined parameters.
		/// </summary>
		/// <returns>The into.</returns>
		/// <param name="parameters">Parameters.</param>
		/// <param name="target">Target.</param>
		/// <typeparam name="TImageView">The 1st type parameter.</typeparam>
		public static IScheduledWork Into<TImageView>(this TaskParameter parameters, ITarget<SelfDisposingBitmapDrawable, TImageView> target, IImageService<SelfDisposingBitmapDrawable> imageService) where TImageView : class
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
		public static Task<IScheduledWork> IntoAsync<TImageView>(this TaskParameter parameters, ITarget<SelfDisposingBitmapDrawable, TImageView> target, IImageService<SelfDisposingBitmapDrawable> imageService) where TImageView : class
		{
			return parameters.IntoAsync(param => param.Into(target, imageService));
		}

		private static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, Action<TaskParameter> into)
		{
			var userErrorCallback = parameters.OnError;
			var finishCallback = parameters.OnFinish;
			var tcs = new TaskCompletionSource<IScheduledWork>();

			parameters
				.Error(ex =>
				{
					tcs.TrySetException(ex);
					userErrorCallback?.Invoke(ex);
				})
				.Finish(scheduledWork =>
				{
					finishCallback?.Invoke(scheduledWork);
					tcs.TrySetResult(scheduledWork);
				});

			into(parameters);

			return tcs.Task;
		}
	}
}
