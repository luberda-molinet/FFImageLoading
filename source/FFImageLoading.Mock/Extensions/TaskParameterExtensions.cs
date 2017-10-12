using System;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Work;
using System.IO;

namespace FFImageLoading
{
    /// <summary>
    /// TaskParameterExtensions
    /// </summary>
    public static class TaskParameterExtensions
    {
        /// <summary>
        /// Invalidate the image corresponding to given parameters from given caches.
        /// </summary>
        /// <param name="parameters">Image parameters.</param>
        /// <param name="cacheType">Cache type.</param>
        public static async Task InvalidateAsync(this TaskParameter parameters, CacheType cacheType)
        {
            using (var task = ImageService.CreateTask(parameters))
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
            var task = ImageService.CreateTask(parameters);
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

            parameters.Preload = true;
            var task = ImageService.CreateTask(parameters);

            var userErrorCallback = parameters.OnError;
            var finishCallback = parameters.OnFinish;

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
        public async static Task DownloadOnlyAsync(this TaskParameter parameters)
        {
            if (parameters.Source == ImageSource.Url)
            {
                await PreloadAsync(parameters.WithCache(CacheType.Disk)).ConfigureAwait(false);
            }
        }
    }
}

