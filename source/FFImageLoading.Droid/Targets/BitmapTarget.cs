using System;
using Android.Graphics.Drawables;
using FFImageLoading.Work;
using FFImageLoading.Drawables;

namespace FFImageLoading.Targets
{
    public class BitmapTarget : Target<ISelfDisposingBitmapDrawable, ISelfDisposingBitmapDrawable>
    {
        WeakReference<ISelfDisposingBitmapDrawable> _drawableWeakReference = null;

        public override void Set(IImageLoaderTask task, ISelfDisposingBitmapDrawable image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            if (_drawableWeakReference == null)
                _drawableWeakReference = new WeakReference<ISelfDisposingBitmapDrawable>(image);
            else
                _drawableWeakReference.SetTarget(image);
        }

        public ISelfDisposingBitmapDrawable BitmapDrawable
        {
            get
            {
                if (_drawableWeakReference == null)
                    return null;

                ISelfDisposingBitmapDrawable drawable = null;
                _drawableWeakReference.TryGetTarget(out drawable);
                var sdDrawable = drawable as ISelfDisposingBitmapDrawable;

                if (sdDrawable != null)
                {
                    sdDrawable?.SetIsDisplayed(true);
                }

                return sdDrawable;
            }
        }
    }
}

