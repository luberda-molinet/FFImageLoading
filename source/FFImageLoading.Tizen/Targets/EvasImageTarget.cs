using System;
using FFImageLoading.Work;
using FFImageLoading.Views;

namespace FFImageLoading.Targets
{            
    class EvasImageTarget : Target<SharedEvasImage, EvasImageContainer>
    {
        readonly WeakReference<EvasImageContainer> _controlWeakReference;

        public EvasImageTarget(EvasImageContainer control)
        {
            _controlWeakReference = new WeakReference<EvasImageContainer>(control);
        }

        public override EvasImageContainer Control
        {
            get
            {
                EvasImageContainer control;
                if (!_controlWeakReference.TryGetTarget(out control))
                    return null;

                if (control == null || control.Handle == IntPtr.Zero)
                    return null;

                return control;
            }
        }

        public override bool IsValid
        {
            get
            {
                return Control != null;
            }
        }

        public override void Set(IImageLoaderTask task, SharedEvasImage image, bool animated)
        {
            if (task == null || task.IsCancelled || !IsValid)
                return;
            image.Show();
            Control.Source = image;
        }

        public override void SetAsEmpty(IImageLoaderTask task)
        {
            if (task == null || task.IsCancelled || !IsValid)
                return;
            Control.Source = null;
        }

    }
}
