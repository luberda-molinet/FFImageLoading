using System;
using FFImageLoading.Work;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Targets
{
    public class ImageTarget : Target<WriteableBitmap, Image>
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

        public override bool IsTaskValid(IImageLoaderTask task)
        {
            return IsValid;
        }

        public override void SetAsEmpty(IImageLoaderTask task)
        {
            var control = Control;
            if (control == null)
                return;

            control.Source = null;
        }

        public override void Set(IImageLoaderTask task, WriteableBitmap image, bool animated)
        {
            if (task.IsCancelled)
                return;

            var control = Control;
            if (control == null || control.Source == image)
                return;

            var parameters = task.Parameters;

            if (animated)
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
                Storyboard.SetTargetProperty(fade, "Image.Opacity");
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

        public override bool UsesSameNativeControl(IImageLoaderTask task)
        {
            var otherTarget = task.Target as ImageTarget;
            if (otherTarget == null)
                return false;

            var control = Control;
            var otherControl = otherTarget.Control;
            if (control == null || otherControl == null)
                return false;

            return control == otherControl;
        }

        public override Image Control
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
