using System;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Work;
using System.IO;
using Microsoft.Maui.Controls;

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
        public static async Task InvalidateAsync<TImageContainer>(this TaskParameter parameters, CacheType cacheType, IImageService<TImageContainer> imageService)
        {
            using (var task = imageService.CreateTask(parameters))
            {
                var key = task.Key;
                await imageService.InvalidateCacheEntryAsync(key, cacheType).ConfigureAwait(false);
            }
        }

		/// <summary>
		/// Preloads the image request into memory cache/disk cache for future use.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
        public static IImageLoaderTask Preload<TImageContainer>(this TaskParameter parameters, IImageService<TImageContainer> imageService)
        {
            if (parameters.Priority == null)
            {
                parameters.WithPriority(LoadingPriority.Low);
            }

            parameters.Preload = true;
            var task = imageService.CreateTask(parameters);
            imageService.LoadImage(task);
            return task;
        }

        /// <summary>
        /// Preloads the image request into memory cache/disk cache for future use.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <param name="parameters">Image parameters.</param>
        public static Task PreloadAsync<TImageContainer>(this TaskParameter parameters, IImageService<TImageContainer> imageService)
        {
            var tcs = new TaskCompletionSource<IScheduledWork>();

            if (parameters.Priority == null)
            {
                parameters.WithPriority(LoadingPriority.Low);
            }

            parameters.Preload = true;
            var task = imageService.CreateTask(parameters);

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

            imageService.LoadImage(task);

            return tcs.Task;
        }

        /// <summary>
        /// Downloads the image request into disk cache for future use if not already exists.
        /// Only Url Source supported.
        /// </summary>
        /// <param name="parameters">Image parameters.</param>
        public static IImageLoaderTask DownloadOnly<TImageContainer>(this TaskParameter parameters, IImageService<TImageContainer> imageService)
        {
            if (parameters.Source == Work.ImageSource.Url)
            {
                return Preload(parameters.WithCache(CacheType.Disk), imageService);
            }

            return null;
        }

        /// <summary>
        /// Downloads the image request into disk cache for future use if not already exists.
        /// Only Url Source supported.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <param name="parameters">Image parameters.</param>
        public async static Task DownloadOnlyAsync<TImageContainer>(this TaskParameter parameters, IImageService<TImageContainer> imageService)
        {
            if (parameters.Source == Work.ImageSource.Url)
            {
                await PreloadAsync(parameters.WithCache(CacheType.Disk), imageService).ConfigureAwait(false);
            }
        }
    }
}
