using System;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using UIKit;

namespace FFImageLoading.Work
{
	public class UIButtonTarget: UIViewTarget<UIButton>
	{
		public UIButtonTarget(UIButton control) : base(control)
		{
		}

		public override void Set(ImageLoaderTask task, UIImage image, bool isLocalOrFromCache, bool isLoadingPlaceholder)
		{
			var control = Control;
			if (control == null)
				return;
			control.SetImage(image, UIControlState.Normal);
		}
	}
}

