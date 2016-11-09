using System;
using System.Threading.Tasks;

using FFImageLoading.Work;
using FFImageLoading.Views;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using Android.Graphics.Drawables;
using System.Collections.Generic;
using FFImageLoading.Drawables;
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
        public static IScheduledWork Into(this TaskParameter parameters, ImageViewAsync imageView)
        {
            imageView.CancelLoading();

            var target = new ImageViewTarget(imageView);

            if (parameters.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
            {
                target.SetAsEmpty(null);
                parameters?.Dispose();
                return null;
            }

            var task = CreateTask(parameters, target);
            ImageService.Instance.LoadImage(task);
            return task;
        }

        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, ImageViewAsync imageView)
        {
            var userErrorCallback = parameters.OnError;
            var finishCallback = parameters.OnFinish;
            var tcs = new TaskCompletionSource<IScheduledWork>();
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
                        tcs.TrySetResult(scheduledWork);
                })
                .Into(imageView);

            return tcs.Task;
        }

        /// <summary>
        /// Loads and gets BitmapDrawable using defined parameters.
        /// IMPORTANT: you should call SetNoLongerDisplayed method if drawable is no longer displayed
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>The bitmap drawable async.</returns>
        /// <param name="parameters">Parameters.</param>
        public static Task<BitmapDrawable> AsBitmapDrawableAsync(this TaskParameter parameters)
        {
            var target = new BitmapTarget();
            var userErrorCallback = parameters.OnError;
            var finishCallback = parameters.OnFinish;
            var tcs = new TaskCompletionSource<BitmapDrawable>();
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
                    tcs.TrySetResult(target.BitmapDrawable as BitmapDrawable);
                });

            if (parameters.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
            {
                target.SetAsEmpty(null);
                parameters?.Dispose();
                return null;
            }

            var task = CreateTask(parameters, target);
            ImageService.Instance.LoadImage(task);

            return tcs.Task;
        }

		/// <summary>
		/// Invalidate the image corresponding to given parameters from given caches.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		/// <param name="cacheType">Cache type.</param>
		public static async Task InvalidateAsync(this TaskParameter parameters, CacheType cacheType)
		{
            var target = new Target<ISelfDisposingBitmapDrawable, object>();
            using (var task = CreateTask(parameters, target))
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
            var target = new Target<ISelfDisposingBitmapDrawable, object>();
            var task = CreateTask(parameters, target);
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

            var target = new Target<ISelfDisposingBitmapDrawable, object>();
            var task = CreateTask(parameters, target);
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

        private static IImageLoaderTask CreateTask<TImageView>(this TaskParameter parameters, ITarget<ISelfDisposingBitmapDrawable, TImageView> target) where TImageView : class
		{
            return new PlatformImageLoaderTask<TImageView>(target, parameters, ImageService.Instance, ImageService.Instance.Config, MainThreadDispatcher.Instance);
		}
    }
}

