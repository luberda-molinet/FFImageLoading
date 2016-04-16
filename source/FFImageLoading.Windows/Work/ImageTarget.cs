using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace FFImageLoading.Work
{
    public class ImageTarget : Target<WriteableBitmap, ImageLoaderTask>
    {
        private readonly WeakReference<Image> _controlWeakReference;

        public ImageTarget(Image control)
        {
            _controlWeakReference = new WeakReference<Image>(control);
        }

        public override bool IsValid
        {
            get
            {
                return Control != null;
            }
        }

        public override bool IsTaskValid(ImageLoaderTask task)
        {
            return IsValid;
        }

        public override void Set(ImageLoaderTask task, WriteableBitmap image, bool isLocalOrFromCache, bool isLoadingPlaceholder)
        {
            if (task.IsCancelled)
                return;

            var control = Control;
            if (control == null)
                return;

            var parameters = task.Parameters;

            bool isFadeAnimationEnabled = parameters.FadeAnimationEnabled ?? ImageService.Instance.Config.FadeAnimationEnabled;
            bool isFadeAnimationEnabledForCached = isFadeAnimationEnabled && (parameters.FadeAnimationForCachedImages ?? ImageService.Instance.Config.FadeAnimationForCachedImages);

            if (!isLoadingPlaceholder && isFadeAnimationEnabled && (!isLocalOrFromCache || (isLocalOrFromCache && isFadeAnimationEnabledForCached)))
            {
                // fade animation
                int fadeDuration = parameters.FadeAnimationDuration.HasValue ?
                    parameters.FadeAnimationDuration.Value : ImageService.Instance.Config.FadeAnimationDuration;
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
                Storyboard.SetTarget(fade, control);
                fadeInStoryboard.Children.Add(fade);
                fadeInStoryboard.Begin();
                control.Source = image;
            }
            else
            {
                control.Source = image;
            }
        }

        public override bool UsesSameNativeControl(ImageLoaderTask task)
        {
            var otherTarget = task._target as ImageTarget;
            if (otherTarget == null)
                return false;

            var control = Control;
            var otherControl = otherTarget.Control;
            if (control == null || otherControl == null)
                return false;

            return control == otherControl;
        }

        private Image Control
        {
            get
            {
                Image control;
                if (!_controlWeakReference.TryGetTarget(out control))
                    return null;

                if (control == null)
                    return null;

                return control;
            }
        }
    }
}
