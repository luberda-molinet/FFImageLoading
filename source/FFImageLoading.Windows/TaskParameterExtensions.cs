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
            var weakRef = new WeakReference<Image>(imageView);

            Func<Image> getNativeControl = () => {
                Image refView = null;

                if (!weakRef.TryGetTarget(out refView))
                    return null;

                return refView;
            };

            Action<WriteableBitmap, bool, bool> doWithImage = (img, isLocalOrFromCache, isLoadingPlaceholder) => {
                Image refView = getNativeControl();
                if (refView == null)
                    return;

                bool imageChanged = (img != refView.Source);
                if (!imageChanged)
                    return;

                bool isFadeAnimationEnabled = parameters.FadeAnimationEnabled.HasValue ?
                    parameters.FadeAnimationEnabled.Value : ImageService.Config.FadeAnimationEnabled;

                bool isFadeAnimationEnabledForCached = isFadeAnimationEnabled && (parameters.FadeAnimationForCachedImages.HasValue ?
                    parameters.FadeAnimationForCachedImages.Value : ImageService.Config.FadeAnimationForCachedImages);

                if (!isLoadingPlaceholder && isFadeAnimationEnabled && (!isLocalOrFromCache || (isLocalOrFromCache && isFadeAnimationEnabledForCached)))
                {
                    // fade animation
                    int fadeDuration = parameters.FadeAnimationDuration.HasValue ?
                        parameters.FadeAnimationDuration.Value : ImageService.Config.FadeAnimationDuration;
                    DoubleAnimation fade = new DoubleAnimation();
                    fade.Duration = TimeSpan.FromMilliseconds(fadeDuration);
					fade.From = 0f;
					fade.To = 1f;
					fade.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }; 

					Storyboard fadeInStoryboard = new Storyboard();

#if SILVERLIGHT
                    Storyboard.SetTargetProperty(fade, new PropertyPath("Image.Opacity"));
#else
                    Storyboard.SetTargetProperty(fade, "Image.Opacity");
#endif
                    Storyboard.SetTarget(fade, refView);
					fadeInStoryboard.Children.Add(fade);
					fadeInStoryboard.Begin();
					refView.Source = img;
                }
                else
                {
                    refView.Source = img;
                }
            };

            return parameters.Into(getNativeControl, doWithImage);
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
		public static void Invalidate(this TaskParameter parameters, CacheType cacheType)
		{
			using (var task = new ImageLoaderTask(ImageService.Config.DownloadCache, new MainThreadDispatcher(), ImageService.Config.Logger, parameters, null, null))
			{
				var key = task.GetKey();
				ImageService.Invalidate(key, cacheType);
			}
		}

        private static IScheduledWork Into(this TaskParameter parameters, Func<Image> getNativeControl, Action<WriteableBitmap, bool, bool> doWithImage)
        {
            var task = new ImageLoaderTask(ImageService.Config.DownloadCache, new MainThreadDispatcher(), ImageService.Config.Logger, parameters,
                getNativeControl, doWithImage);
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
