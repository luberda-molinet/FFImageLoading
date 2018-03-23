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

            var isLayoutNeeded = IsLayoutNeeded(task, control.Image, image);
            
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

                        if (isLayoutNeeded)
                            control.SetNeedsLayout(); // It's needed for cells, etc
                    },
                    () => { });
            }
            else
            {
                if (control.Image?.Images != null && control.Image.Images.Length > 1)
                    control.Image = null;
                control.Image = image;

                if (isLayoutNeeded)
                    control.SetNeedsLayout(); // It's needed for cells, etc
            }
        }

        bool IsLayoutNeeded(IImageLoaderTask task, UIImage oldImage, UIImage newImage)
        {
            if (task.Parameters.InvalidateLayoutEnabled.HasValue)
            {
                if (!task.Parameters.InvalidateLayoutEnabled.Value)
                    return false;
            }
            else if (!task.Configuration.InvalidateLayout)
            {
                return false;
            }

            try
            {
                if (oldImage == null && newImage == null)
                    return false;

                if (oldImage == null && newImage != null)
                    return true;

                if (oldImage != null && newImage == null)
                    return true;

                if (oldImage != null && newImage != null)
                {
                    return !(oldImage.Size.Width == newImage.Size.Width && oldImage.Size.Height == newImage.Size.Height);
                }
            }
            catch (Exception)
            {
                return true;
            }

            return false;
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
