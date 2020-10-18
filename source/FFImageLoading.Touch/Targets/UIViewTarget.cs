using System;
using FFImageLoading.Work;
using ObjCRuntime;
using UIKit;
using Foundation;

namespace FFImageLoading.Targets
{
    public class UIViewTarget<TView> : Target<UIImage, TView> where TView : NSObject, INativeObject
    {
        protected readonly WeakReference<TView> _controlWeakReference;

        protected UIViewTarget(TView control)
        {
            _controlWeakReference = new WeakReference<TView>(control);
        }

        public override bool IsValid
        {
            get
            {
                try
                {
                    return Control != null && Control.Handle != IntPtr.Zero;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
        }

        public override TView Control
        {
            get
            {
                if (!_controlWeakReference.TryGetTarget(out var control))
                    return null;

                if (control == null || control.Handle == IntPtr.Zero)
                    return null;

                return control;
            }
        }
    }

}
