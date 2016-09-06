using System;
using Android.Graphics.Drawables;
using FFImageLoading.Work;

namespace FFImageLoading
{
    public class BitmapTarget : Target<BitmapDrawable, ImageLoaderTask>
    {
        private WeakReference<BitmapDrawable> _drawableWeakReference = null;

        public override void Set(ImageLoaderTask task, BitmapDrawable image, bool isLocalOrFromCache, bool isLoadingPlaceholder)
        {
            if (task.IsCancelled)
                return;

            if (!isLoadingPlaceholder)
            {
                if (_drawableWeakReference == null)
                    _drawableWeakReference = new WeakReference<BitmapDrawable>(image);
                else
                    _drawableWeakReference.SetTarget(image);
            }
        }

        public BitmapDrawable BitmapDrawable
        {
            get
            {
                if (_drawableWeakReference == null)
                    return null;

                BitmapDrawable drawable = null;
                _drawableWeakReference.TryGetTarget(out drawable);
                return drawable;
            }
        }
    }
}

