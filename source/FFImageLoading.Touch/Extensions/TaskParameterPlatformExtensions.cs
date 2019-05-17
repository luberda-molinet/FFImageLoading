using System;
using System.Threading.Tasks;
using FFImageLoading.Work;
using System.IO;
using UIKit;
using FFImageLoading.Targets;
using FFImageLoading.Extensions;
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
        public static async Task<Stream> AsPNGStreamAsync(this TaskParameter parameters)
        {
            var result = await AsUIImageAsync(parameters).ConfigureAwait(false);
            return result.AsPngStream();
        }

        /// <summary>
        /// Loads the image into JPG Stream
        /// </summary>
        /// <returns>The JPG Stream async.</returns>
        /// <param name="parameters">Parameters.</param>
        /// <param name="quality">Quality.</param>
        public static async Task<Stream> AsJPGStreamAsync(this TaskParameter parameters, int quality = 80)
        {
            var result = await AsUIImageAsync(parameters).ConfigureAwait(false);
            return result.AsJpegStream(quality);
        }

        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static IScheduledWork Into(this TaskParameter parameters, UIImageView imageView)
        {
            var target = new UIImageViewTarget(imageView);

            return parameters.Into(target);
        }

        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="item">Image view that should receive the image.</param>
        public static IScheduledWork Into(this TaskParameter parameters, UITabBarItem item)
        {
            var target = new UIBarItemTarget(item);
            return parameters.Into(target);
        }

        /// <summary>
        /// Loads the image into given UIButton using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="button">UIButton that should receive the image.</param>
        public static IScheduledWork Into(this TaskParameter parameters, UIButton button)
        {
            var target = new UIButtonTarget(button);
            return parameters.Into(target);
        }

        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, UIImageView imageView)
        {
            return parameters.IntoAsync(param => param.Into(imageView));
        }

        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="item">Image view that should receive the image.</param>
        public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, UITabBarItem item)
        {
            return parameters.IntoAsync(param => param.Into(item));
        }

        /// <summary>
        /// Loads the image into given UIButton using defined parameters.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="button">UIButton that should receive the image.</param>
        public static Task<IScheduledWork> IntoAsync(this TaskParameter parameters, UIButton button)
        {
            return parameters.IntoAsync(param => param.Into(button));
        }

        /// <summary>
        /// Loads and gets UImage using defined parameters.
        /// IMPORTANT: It throws image loading exceptions - you should handle them
        /// </summary>
        /// <returns>The PImage async.</returns>
        /// <param name="parameters">Parameters.</param>
        public static Task<UIImage> AsUIImageAsync(this TaskParameter parameters)
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

            if (parameters.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(parameters.Path))
            {
                target.SetAsEmpty(null);
                parameters.TryDispose();
                return null;
            }

            var task = ImageService.CreateTask(parameters, target);
            ImageService.Instance.LoadImage(task);

            return tcs.Task;
        }

        /// <summary>
        /// Loads the image into given target using defined parameters.
        /// </summary>
        /// <returns>The into.</returns>
        /// <param name="parameters">Parameters.</param>
        /// <param name="target">Target.</param>
        /// <typeparam name="TImageView">The 1st type parameter.</typeparam>
        public static IScheduledWork Into<TImageView>(this TaskParameter parameters, ITarget<UIImage, TImageView> target) where TImageView : class
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
        public static Task<IScheduledWork> IntoAsync<TImageView>(this TaskParameter parameters, ITarget<UIImage, TImageView> target) where TImageView : class
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
