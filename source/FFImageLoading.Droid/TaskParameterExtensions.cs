using System;
using System.Threading.Tasks;

using FFImageLoading.Work;
using FFImageLoading.Views;
using FFImageLoading.Helpers;
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
        public static IScheduledWork Into(this TaskParameter parameters, ImageViewAsync imageView)
        {
			var task = CreateTask(parameters, imageView);
            ImageService.Instance.LoadImage(task);
            return task;
        }

        /// <summary>
        /// Only use this method if you plan to handle exceptions in your code. Awaiting this method will give you this flexibility.
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, ImageViewAsync imageView)
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
                })
                .Into(imageView);

            return tcs.Task;
        }

		/// <summary>
		/// Invalidate the image corresponding to given parameters from given caches.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		/// <param name="cacheType">Cache type.</param>
		public static async Task InvalidateAsync(this TaskParameter parameters, CacheType cacheType)
		{
			using (var task = CreateTask(parameters, null))
			{
				var key = task.GetKey();
				await ImageService.Instance.InvalidateCacheEntryAsync(key, cacheType).ConfigureAwait(false);
			}
		}

		private static ImageLoaderTask CreateTask(this TaskParameter parameters, ImageViewAsync imageView)
		{
			var task = new ImageLoaderTask(ImageService.Instance.Config.DownloadCache, new MainThreadDispatcher(), ImageService.Instance.Config.Logger, parameters, imageView);
			return task;
		}
    }
}

