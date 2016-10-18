using System;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using FFImageLoading.Work;
using UIKit;

namespace FFImageLoading.Targets
{
	public class UIButtonTarget: UIControlTarget<UIButton>
	{
		public UIButtonTarget(UIButton control) : base(control)
		{
		}

        public override void Set(IImageLoaderTask task, UIImage image, bool animated)
        {
            task.CancellationToken.ThrowIfCancellationRequested();

            var control = Control;
            if (control == null)
                return;
            control.SetImage(image, UIControlState.Normal);
        }

        public override void SetAsEmpty(IImageLoaderTask task)
        {
            task.CancellationToken.ThrowIfCancellationRequested();

            var control = Control;
            if (control == null)
                return;

            control.SetImage(null, UIControlState.Normal);
        }
	}
}

