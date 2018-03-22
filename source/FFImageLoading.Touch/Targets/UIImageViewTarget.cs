using System;
using FFImageLoading.Work;
using UIKit;

namespace FFImageLoading.Targets
{
    public class UIImageViewTarget : UIViewTarget<UIImageView>
    {
        public UIImageViewTarget(UIImageView control) : base(control)
        {
        }

        public override void Set(IImageLoaderTask task, UIImage image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            var control = Control;

            if (control == null) return;
            if (control.Image == image && (control.Image?.Images == null || control.Image.Images.Length <= 1))
                return;
            
            var parameters = task.Parameters;
            if (animated)
            {
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
                        control.SetNeedsLayout(); // It's needed for cells, etc
                        // control.SetNeedsDisplay();
                    },
                    () => { });
            }
            else
            {
                if (control.Image?.Images != null && control.Image.Images.Length > 1)
                    control.Image = null;
                control.Image = image;
                control.SetNeedsLayout(); // It's needed for cells, etc
                // control.SetNeedsDisplay();
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
