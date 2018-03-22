using System;
using AppKit;
using FFImageLoading.Work;

namespace FFImageLoading.Targets
{
    public class NSImageViewTarget : NSViewTarget<NSImageView>
    {
        public NSImageViewTarget(NSImageView control) : base(control)
        {
        }

        public override void Set(IImageLoaderTask task, NSImage image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            var control = Control;

            if (control == null) return;
            if (control.Layer.Contents == image.CGImage) return;

            var parameters = task.Parameters;
            var representations = image.Representations();

            if (representations.Length > 1)
            {
                control.Layer.Contents = null;
                control.Image = image;
                control.Animates = true;
                control.NeedsLayout = true;
                control.SetNeedsDisplay();
            }
            else
            {
                if (animated)
                {
                    //TODO fade animation
                    control.Layer.Contents = image.CGImage;
                    control.NeedsLayout = true;
                    control.SetNeedsDisplay();
                }
                else
                {
                    control.Layer.Contents = image.CGImage;
                    control.NeedsLayout = true;
                    control.SetNeedsDisplay();
                }
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
