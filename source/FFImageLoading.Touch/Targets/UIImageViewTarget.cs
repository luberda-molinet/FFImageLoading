using System;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using FFImageLoading.Work;
using UIKit;

namespace FFImageLoading.Targets
{
	public class UIImageViewTarget: UIControlTarget<UIImageView>
	{
		public UIImageViewTarget(UIImageView control) : base(control)
		{
		}

        public override void Set(IImageLoaderTask task, UIImage image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            var control = Control;
            if (control == null || control.Image == image)
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
                    () => { control.Image = image; },
                    () => { });
            }
            else
            {
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

