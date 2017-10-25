using System;
using FFImageLoading.Work;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Targets
{
    public class BitmapTarget: Target<BitmapSource, WriteableBitmap>
    {
        private WeakReference<BitmapSource> _imageWeakReference = null;

        public override void Set(IImageLoaderTask task, BitmapSource image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            if (_imageWeakReference == null)
                _imageWeakReference = new WeakReference<BitmapSource>(image);
            else
                _imageWeakReference.SetTarget(image);
        }

        public BitmapSource BitmapSource
        {
            get
            {
                if (_imageWeakReference == null)
                    return null;

                BitmapSource image = null;
                _imageWeakReference.TryGetTarget(out image);
                return image;
            }
        }
    }
}
