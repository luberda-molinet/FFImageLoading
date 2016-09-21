using System;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using UIKit;

namespace FFImageLoading.Work
{
	public class UIBarItemTarget: UIControlTarget<UIBarItem>
	{
        // For some reason the UIBarItem .NET instance can be garbaged even though the control still exists (especially with UITabBarItem).
        // So we keep a strong reference for that case.
        private readonly UIBarItem _controlStrongReference;

        public UIBarItemTarget(UIBarItem control) : base(control)
		{
            _controlStrongReference = control;
		}

		public override void Set(ImageLoaderTask task, UIImage image, bool isLocalOrFromCache, bool isLoadingPlaceholder)
		{
			var control = Control;
			if (control == null)
				return;

			control.Image = image;
		}

        public override void SetAsEmpty(ImageLoaderTask task)
        {
            var control = Control;
            if (control == null)
                return;

            control.Image = null;
        }
	}
}

