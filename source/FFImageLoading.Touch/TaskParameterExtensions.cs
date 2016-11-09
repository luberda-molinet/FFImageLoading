using System;
using System.Threading.Tasks;
using FFImageLoading.Work;
using FFImageLoading.Helpers;
using UIKit;
using FFImageLoading.Cache;
using System.Collections.Generic;
using FFImageLoading.Targets;

namespace FFImageLoading
{
    public static class TaskParameterExtensions
    {
        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        /// <param name="imageScale">Optional scale factor to use when interpreting the image data. If unspecified it will use the device scale (ie: Retina = 2, non retina = 1)</param>
        public static IScheduledWork Into(this TaskParameter parameters, UIImageView imageView, float imageScale = -1f)
        {
            var target = new UIImageViewTarget(imageView);
			return parameters.Into(imageScale, target);
        }

        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        /// <param name="imageScale">Optional scale factor to use when interpreting the image data. If unspecified it will use the device scale (ie: Retina = 2, non retina = 1)</param>
        public static IScheduledWork Into(this TaskParameter parameters, UITabBarItem item, float imageScale = -1f)
        {
            var target = new UIBarItemTarget(item);
            return parameters.Into(imageScale, target);
        }

        /// <summary>
        /// Loads the image into given UIButton using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="button">UIButton that should receive the image.</param>
        /// <param name="imageScale">Optional scale factor to use when interpreting the image data. If unspecified it will use the device scale (ie: Retina = 2, non retina = 1)</param>
        public static IScheduledWork Into(this TaskParameter parameters, UIButton button, float imageScale = -1f)
        {
			var target = new UIButtonTarget(button);
            return parameters.Into(imageScale, target);
        }

        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        /// <param name="imageScale">Optional scale factor to use when interpreting the image data. If unspecified it will use the device scale (ie: Retina = 2, non retina = 1)</param>
        public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, UIImageView imageView, float imageScale = -1f)
        {
            return parameters.IntoAsync(param => param.Into(imageView, imageScale));
        }

        /// <summary>
        /// Loads and gets UIImage using defined parameters.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>The UIImage async.</returns>
        /// <param name="parameters">Parameters.</param>
        /// <param name="imageScale">Image scale.</param>
        public static Task<UIImage> AsUIImageAsync(this TaskParameter parameters, float imageScale = -1f)
        {
            var target = new UIImageTarget();
            var userErrorCallback = parameters.OnError;
            var finishCallback = parameters.OnFinish;
            var tcs = new TaskCompletionSource<UIImage>();
            List<Exception> exceptions = null;

            parameters
                .Error(ex =>
                {
                    if (exceptions == null)
                        exceptions = new List<Exception>();

                    exceptions.Add(ex);
                    userErrorCallback?.Invoke(ex);
                })
                .Finish(scheduledWork =>
                {
                    finishCallback?.Invoke(scheduledWork);

                    if (exceptions != null)
                        tcs.TrySetException(exceptions);
                    else
                        tcs.TrySetResult(target.UIImage);
                });

            if (parameters.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
            {
                target.SetAsEmpty(null);
                parameters?.Dispose();
                return null;
            }

            var task = CreateTask(parameters, imageScale, target);
            ImageService.Instance.LoadImage(task);

            return tcs.Task;
        }

        /// <summary>
        /// Loads the image into given UIButton using defined parameters.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="button">UIButton that should receive the image.</param>
        /// <param name="imageScale">Optional scale factor to use when interpreting the image data. If unspecified it will use the device scale (ie: Retina = 2, non retina = 1)</param>
        public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, UIButton button, float imageScale = -1f)
        {
            return parameters.IntoAsync(param => param.Into(button, imageScale));
        }

		/// <summary>
		/// Invalidate the image corresponding to given parameters from given caches.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		/// <param name="cacheType">Cache type.</param>
		public static async Task InvalidateAsync(this TaskParameter parameters, CacheType cacheType)
		{
            var target = new Target<UIImage, object>();
			using (var task = CreateTask(parameters, 1, target))
			{
                var key = task.Key;
				await ImageService.Instance.InvalidateCacheEntryAsync(key, cacheType).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Preloads the image request into memory cache/disk cache for future use.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		public static IImageLoaderTask Preload(this TaskParameter parameters)
		{
            if (parameters.Priority == null)
            {
                parameters.WithPriority(LoadingPriority.Low);
            }

			parameters.Preload = true;
            var target = new Target<UIImage, object>();
			var task = CreateTask(parameters, 1f, target);
			ImageService.Instance.LoadImage(task);
            return task;
		}

        /// <summary>
        /// Preloads the image request into memory cache/disk cache for future use.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <param name="parameters">Image parameters.</param>
        public static Task PreloadAsync(this TaskParameter parameters)
        {
            var tcs = new TaskCompletionSource<IScheduledWork>();

            if (parameters.Priority == null)
            {
                parameters.WithPriority(LoadingPriority.Low);
            }

            var userErrorCallback = parameters.OnError;
            var finishCallback = parameters.OnFinish;
            List<Exception> exceptions = null;

            parameters.Preload = true;

            parameters
            .Error(ex =>
            {
                if (exceptions == null)
                    exceptions = new List<Exception>();

                exceptions.Add(ex);
                userErrorCallback?.Invoke(ex);
            })
            .Finish(scheduledWork =>
            {
                finishCallback?.Invoke(scheduledWork);

                if (exceptions != null)
                    tcs.TrySetException(exceptions);
                else
                    tcs.TrySetResult(scheduledWork);
            });

            var target = new Target<UIImage, object>();
            var task = CreateTask(parameters, 1f, target);
            ImageService.Instance.LoadImage(task);

            return tcs.Task;
        }

        /// <summary>
        /// Downloads the image request into disk cache for future use if not already exists.
        /// Only Url Source supported.
        /// </summary>
        /// <param name="parameters">Image parameters.</param>
        public static IImageLoaderTask DownloadOnly(this TaskParameter parameters)
        {
            if (parameters.Source == ImageSource.Url)
            {
                return Preload(parameters.WithCache(CacheType.Disk));
            }

            return null;
        }

        /// <summary>
        /// Downloads the image request into disk cache for future use if not already exists.
        /// Only Url Source supported.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <param name="parameters">Image parameters.</param>
        public static async Task DownloadOnlyAsync(this TaskParameter parameters)
        {
            if (parameters.Source == ImageSource.Url)
            {
                await PreloadAsync(parameters.WithCache(CacheType.Disk));
            }
        }

        private static IImageLoaderTask CreateTask<TImageView>(this TaskParameter parameters, float imageScale, ITarget<UIImage, TImageView> target) where TImageView: class
		{
            return new PlatformImageLoaderTask<TImageView>(target, parameters, ImageService.Instance, ImageService.Instance.Config, MainThreadDispatcher.Instance);
		}

        private static IScheduledWork Into<TImageView>(this TaskParameter parameters, float imageScale, ITarget<UIImage, TImageView> target) where TImageView : class
        {
            if (parameters.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
            {
                target.SetAsEmpty(null);
                parameters?.Dispose();
                return null;
            }

			var task = CreateTask(parameters, imageScale, target);
			ImageService.Instance.LoadImage(task);
            return task;
        }

        private static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, Action<TaskParameter> into)
        {
            var userErrorCallback = parameters.OnError;
            var finishCallback = parameters.OnFinish;
            var tcs = new TaskCompletionSource<IScheduledWork>();
            List<Exception> exceptions = null;

            parameters
                .Error(ex => {
                    if (exceptions == null)
                        exceptions = new List<Exception>();

                    exceptions.Add(ex);
                    userErrorCallback?.Invoke(ex);
                })
                .Finish(scheduledWork => {
                    finishCallback?.Invoke(scheduledWork);

                    if (exceptions != null)
                        tcs.TrySetException(exceptions);
                    else
                        tcs.TrySetResult(scheduledWork);
                });

            into(parameters);

            return tcs.Task;
        }
    }
}

