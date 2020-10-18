using System;
using FFImageLoading.Work;
using FFImageLoading.Drawables;

namespace FFImageLoading.Targets
{
    public class BitmapTarget : Target<SelfDisposingBitmapDrawable, ISelfDisposingBitmapDrawable>
    {
        private WeakReference<SelfDisposingBitmapDrawable> _drawableWeakReference;

        public override void Set(IImageLoaderTask task, SelfDisposingBitmapDrawable image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            if (_drawableWeakReference == null)
                _drawableWeakReference = new WeakReference<SelfDisposingBitmapDrawable>(image);
            else
                _drawableWeakReference.SetTarget(image);
        }

        public SelfDisposingBitmapDrawable BitmapDrawable
        {
            get
            {
                if (_drawableWeakReference == null)
                    return null;

                _drawableWeakReference.TryGetTarget(out var drawable);
                var sdDrawable = drawable as SelfDisposingBitmapDrawable;
                sdDrawable?.SetIsDisplayed(true);

                return sdDrawable;
            }
        }
    }
}

