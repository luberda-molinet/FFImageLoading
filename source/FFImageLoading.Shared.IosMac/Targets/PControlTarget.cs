using System;
using FFImageLoading.Work;
using ObjCRuntime;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif

namespace FFImageLoading.Targets
{
    public abstract class PControlTarget<TControl> : Target<PImage, TControl> where TControl: class, INativeObject
    {
        protected readonly WeakReference<TControl> _controlWeakReference;

        protected PControlTarget(TControl control)
        {
            _controlWeakReference = new WeakReference<TControl>(control);
        }

        public override bool IsValid
        {
            get
            {
                return Control != null && Control.Handle != IntPtr.Zero;;
            }
        }

        public override bool IsTaskValid(IImageLoaderTask task)
        {
            return IsValid;
        }

        public override bool UsesSameNativeControl(IImageLoaderTask task)
        {
            var otherTarget = task.Target as PControlTarget<TControl>;
            if (otherTarget == null)
                return false;

            var control = Control;
            var otherControl = otherTarget.Control;
            if (control == null || otherControl == null)
                return false;

            return control.Handle == otherControl.Handle;
        }

        public override TControl Control
        {
            get
            {
                TControl control;
                if (!_controlWeakReference.TryGetTarget(out control))
                    return null;

                if (control == null || control.Handle == IntPtr.Zero)
                    return null;

                return control;
            }
        }
    }
}

