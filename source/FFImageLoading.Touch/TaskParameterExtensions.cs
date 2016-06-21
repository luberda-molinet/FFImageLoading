using System;
using System.Threading.Tasks;

using FFImageLoading.Work;
using FFImageLoading.Helpers;
using UIKit;
using CoreAnimation;
using FFImageLoading.Cache;


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
        /// Only use this method if you plan to handle exceptions in your code. Awaiting this method will give you this flexibility.
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
        /// Only use this method if you plan to handle exceptions in your code. Awaiting this method will give you this flexibility.
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
			var target = new Target<UIImage, ImageLoaderTask>();
			using (var task = CreateTask(parameters, 1, target))
			{
				var key = task.GetKey();
				await ImageService.Instance.InvalidateCacheEntryAsync(key, cacheType).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Preload the image request into memory cache/disk cache for future use.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		public static void Preload(this TaskParameter parameters)
		{
            if (parameters.Priority == null)
            {
                parameters.WithPriority(LoadingPriority.Low);
            }

			parameters.Preload = true;
			var target = new Target<UIImage, ImageLoaderTask>();
			var task = CreateTask(parameters, 1, target);
			ImageService.Instance.LoadImage(task);
		}

		private static ImageLoaderTask CreateTask(this TaskParameter parameters, float imageScale, ITarget<UIImage, ImageLoaderTask> target)
		{
            return new ImageLoaderTask(ImageService.Instance.Config.DownloadCache, MainThreadDispatcher.Instance, ImageService.Instance.Config.Logger, parameters, imageScale, target, ImageService.Instance.Config.VerboseLoadingCancelledLogging);
		}

		private static IScheduledWork Into(this TaskParameter parameters, float imageScale, ITarget<UIImage, ImageLoaderTask> target)
        {
            if (parameters.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
            {
                target.SetAsEmpty();
                parameters.Dispose();
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

            parameters
                .Error(ex => {
                    userErrorCallback(ex);
                    tcs.SetException(ex);
                })
                .Finish(scheduledWork => {
                    finishCallback(scheduledWork);
                    tcs.TrySetResult(scheduledWork); // we should use TrySetResult since SetException could have been called earlier. It is not allowed to set result after SetException
                });

            into(parameters);

            return tcs.Task;
        }
    }
}

