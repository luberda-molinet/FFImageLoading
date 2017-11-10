using System;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using FFImageLoading.Work;
#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
using PImageView = AppKit.NSImageView;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
using PImageView = UIKit.UIImageView;
#endif

namespace FFImageLoading.Targets
{
    public class PImageViewTarget : PControlTarget<PImageView>
    {
        public PImageViewTarget(PImageView control) : base(control)
        {
        }

        public override void Set(IImageLoaderTask task, PImage image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            var control = Control;

            if (control == null) return;
#if __MACOS__
            if (control.Image == image) return;
#elif __IOS__
            if (control.Image == image && (control.Image?.Images == null || control.Image.Images.Length <= 1))
                return;
#endif
            var parameters = task.Parameters;
            if (animated)
            {
#if __MACOS__
                // no animation support on Mac. NSImageView does not support animation like UIImageView does
                control.Image = image;
#elif __IOS__
                // fade animation
                double fadeDuration = (double)((parameters.FadeAnimationDuration.HasValue ?
                    parameters.FadeAnimationDuration.Value : ImageService.Instance.Config.FadeAnimationDuration)) / 1000;

                UIView.Transition(control, fadeDuration,
                    UIViewAnimationOptions.TransitionCrossDissolve
                    | UIViewAnimationOptions.BeginFromCurrentState,
                    () =>
                    {
                        if (control.Image?.Images != null && control.Image.Images.Length > 1)
                            control.Image = null;

                        control.Image = image;
                    },
                    () => { });
#endif
            }
            else
            {
#if __IOS__
                if (control.Image?.Images != null && control.Image.Images.Length > 1)
                    control.Image = null;
#endif
                control.Image = image;
            }
        }

        public override void SetAsEmpty(IImageLoaderTask task)
        {
            if (task == null || task.IsCancelled)
                return;

            var control = Control;
            if (control == null)
                return;

            control.Image = null;
        }
    }
}

