using System.Threading.Tasks;
using FFImageLoading.Work;
using FFImageLoading.Views;
using FFImageLoading.Targets;
using System;

namespace FFImageLoading.Extensions
{
    /// <summary>
    /// TaskParameterPlatformExtensions
    /// </summary>
    public static class TaskParameterPlatformExtensions
    {
        /// <summary>
        /// Loads the image into given ImageViewAsync using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static IScheduledWork Into(this TaskParameter parameters, EvasImageContainer imageView)
        {
            var target = new EvasImageTarget(imageView);
            return parameters.Into(target);
        }

        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, EvasImageContainer imageView)
        {
            return parameters.IntoAsync(param => param.Into(imageView));
        }

        /// <summary>
        /// Loads the image into given target using defined parameters.
        /// </summary>
        /// <returns>The into.</returns>
        /// <param name="parameters">Parameters.</param>
        /// <param name="target">Target.</param>
        /// <typeparam name="TImageView">The 1st type parameter.</typeparam>
        public static IScheduledWork Into<TImageView>(this TaskParameter parameters, ITarget<SharedEvasImage, TImageView> target) where TImageView : class
        {
            if (parameters.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
            {
                target.SetAsEmpty(null);
                parameters.TryDispose();
                return null;
            }

            var task = ImageService.CreateTask(parameters, target);
            ImageService.Instance.LoadImage(task);
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
        public static Task<IScheduledWork> IntoAsync<TImageView>(this TaskParameter parameters, ITarget<SharedEvasImage, TImageView> target) where TImageView : class
        {
            return parameters.IntoAsync(param => param.Into(target));
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