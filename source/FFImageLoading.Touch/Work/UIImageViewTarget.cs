using System;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using UIKit;

namespace FFImageLoading.Work
{
	public class UIImageViewTarget: UIControlTarget<UIImageView>
	{
		public UIImageViewTarget(UIImageView control) : base(control)
		{
		}

		public override void Set(ImageLoaderTask task, UIImage image, bool isLocalOrFromCache, bool isLoadingPlaceholder)
		{
			var control = Control;
			if (control == null)
				return;

			var parameters = task.Parameters;

			bool isFadeAnimationEnabled = parameters.FadeAnimationEnabled ?? ImageService.Instance.Config.FadeAnimationEnabled;
			bool isFadeAnimationEnabledForCached = isFadeAnimationEnabled && (parameters.FadeAnimationForCachedImages ?? ImageService.Instance.Config.FadeAnimationForCachedImages);

			if (!isLoadingPlaceholder && isFadeAnimationEnabled && (!isLocalOrFromCache || (isLocalOrFromCache && isFadeAnimationEnabledForCached)))
			{
				// fade animation
				double fadeDuration = (double)((parameters.FadeAnimationDuration.HasValue ?
					parameters.FadeAnimationDuration.Value : ImageService.Instance.Config.FadeAnimationDuration)) / 1000;

				UIView.Transition(control, fadeDuration, 
					UIViewAnimationOptions.TransitionCrossDissolve 
					| UIViewAnimationOptions.BeginFromCurrentState,
					() => { control.Image = image; },
					() => {  });
			}
			else
			{
				control.Image = image;
			}
		}

        public override void SetAsEmpty()
        {
            var control = Control;
            if (control == null)
                return;

            control.Image = null;
        }
	}
}

