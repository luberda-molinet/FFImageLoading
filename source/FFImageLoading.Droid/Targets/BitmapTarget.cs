using System;
using Android.Graphics.Drawables;
using FFImageLoading.Work;
using FFImageLoading.Drawables;

namespace FFImageLoading.Targets
{
    public class BitmapTarget : Target<SelfDisposingBitmapDrawable, SelfDisposingBitmapDrawable>
    {
        WeakReference<BitmapDrawable> _drawableWeakReference = null;

        public override void Set(IImageLoaderTask task, SelfDisposingBitmapDrawable image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            if (_drawableWeakReference == null)
                _drawableWeakReference = new WeakReference<BitmapDrawable>(image);
            else
                _drawableWeakReference.SetTarget(image);
        }

        public SelfDisposingBitmapDrawable BitmapDrawable
        {
            get
            {
                if (_drawableWeakReference == null)
                    return null;

                BitmapDrawable drawable = null;
                _drawableWeakReference.TryGetTarget(out drawable);
                var sdDrawable = drawable as SelfDisposingBitmapDrawable;

                if (sdDrawable != null)
                {
                    sdDrawable?.SetIsDisplayed(true);
                }

                return sdDrawable;
            }
        }
    }
}

