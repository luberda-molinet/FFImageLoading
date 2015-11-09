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
            var weakRef = new WeakReference<UIImageView>(imageView);
            Func<UIImageView> getNativeControl = () => {
                UIImageView refView;
                if (!weakRef.TryGetTarget(out refView))
                    return null;
                return refView;
            };

			Action<UIImage, bool> doWithImage = (img, fromCache) => {
                UIImageView refView = getNativeControl();
                if (refView == null)
                    return;

				var isFadeAnimationEnabled = parameters.FadeAnimationEnabled.HasValue ? 
					parameters.FadeAnimationEnabled.Value : ImageService.Config.FadeAnimationEnabled;

				if (isFadeAnimationEnabled && !fromCache)
				{
					// fade animation
					UIView.Transition(refView, 0.4f, UIViewAnimationOptions.TransitionCrossDissolve,
						() => { refView.Image = img; },
						() => {  });
				}
				else
				{
					refView.Image = img;
				}
            };

            return parameters.Into(getNativeControl, doWithImage, imageScale);
        }

        /// <summary>
        /// Loads the image into given UIButton using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="button">UIButton that should receive the image.</param>
        /// <param name="imageScale">Optional scale factor to use when interpreting the image data. If unspecified it will use the device scale (ie: Retina = 2, non retina = 1)</param>
        public static IScheduledWork Into(this TaskParameter parameters, UIButton button, float imageScale = -1f)
        {
            var weakRef = new WeakReference<UIButton>(button);
            Func<UIButton> getNativeControl = () => {
                UIButton refView;
                if (!weakRef.TryGetTarget(out refView))
                    return null;
                return refView;
            };

			Action<UIImage, bool> doWithImage = (img, fromCache) => {
                UIButton refView = getNativeControl();
                if (refView == null)
                    return;
                refView.SetImage(img, UIControlState.Normal);
            };
            return parameters.Into(getNativeControl, doWithImage, imageScale);
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

        private static IScheduledWork Into(this TaskParameter parameters, Func<UIView> getNativeControl, Action<UIImage, bool> doWithImage, float imageScale = -1f)
        {
            var task = new ImageLoaderTask(ImageService.Config.DownloadCache, new MainThreadDispatcher(), ImageService.Config.Logger, parameters,
                getNativeControl, doWithImage, imageScale);
            ImageService.LoadImage(task);
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

