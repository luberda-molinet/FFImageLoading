using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using System;
using System.Threading.Tasks;

#if SILVERLIGHT
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows;
#else
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
#endif

namespace FFImageLoading
{
    public static class TaskParameterExtensions
    {
        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static IScheduledWork Into(this TaskParameter parameters, Image imageView)
        {
            var target = new ImageTarget(imageView);
            return parameters.Into(target);
        }

        /// <summary>
        /// Only use this method if you plan to handle exceptions in your code. Awaiting this method will give you this flexibility.
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, Image imageView)
        {
            return parameters.IntoAsync(param => param.Into(imageView));
        }

		/// <summary>
		/// Invalidate the image corresponding to given parameters from given caches.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		/// <param name="cacheType">Cache type.</param>
		public static async Task InvalidateAsync(this TaskParameter parameters, CacheType cacheType)
		{
			var target = new Target<WriteableBitmap, ImageLoaderTask>();
			using (var task = CreateTask(parameters, target))
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
            var target = new Target<WriteableBitmap, ImageLoaderTask>();
            var task = CreateTask(parameters, target);
            ImageService.Instance.LoadImage(task);
        }

        private static IScheduledWork Into(this TaskParameter parameters, ITarget<WriteableBitmap, ImageLoaderTask> target)
        {
            if (parameters.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
            {
                target.SetAsEmpty();
                parameters.Dispose();
                return null;
            }

            var task = CreateTask(parameters, target);
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

        private static ImageLoaderTask CreateTask(this TaskParameter parameters, ITarget<WriteableBitmap, ImageLoaderTask> target)
        {
            return new ImageLoaderTask(ImageService.Instance.Config.DownloadCache, MainThreadDispatcher.Instance, ImageService.Instance.Config.Logger, parameters, target, ImageService.Instance.Config.VerboseLoadingCancelledLogging);
        }
    }
}
