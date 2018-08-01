using System;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using UIKit;

namespace FFImageLoading.Targets
{
    public class UIBarItemTarget: UIViewTarget<UIBarItem>
    {
        // For some reason the UIBarItem .NET instance can be garbaged even though the control still exists (especially with UITabBarItem).
        // So we keep a strong reference for that case.
        #pragma warning disable 0414
        private readonly UIBarItem _controlStrongReference;
        #pragma warning restore 0414

        public UIBarItemTarget(UIBarItem control) : base(control)
        {
            _controlStrongReference = control;
        }

        public override void Set(Work.IImageLoaderTask task, UIImage image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            var control = Control;
            if (control == null)
                return;

            control.Image = image;
        }

        public override void SetAsEmpty(Work.IImageLoaderTask task)
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

