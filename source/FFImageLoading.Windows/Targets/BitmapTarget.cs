using System;
using FFImageLoading.Work;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Targets
{
    public class BitmapTarget: Target<WriteableBitmap, WriteableBitmap>
    {
        private WeakReference<WriteableBitmap> _imageWeakReference = null;

        public override void Set(IImageLoaderTask task, WriteableBitmap image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            if (_imageWeakReference == null)
                _imageWeakReference = new WeakReference<WriteableBitmap>(image);
            else
                _imageWeakReference.SetTarget(image);
        }

        public WriteableBitmap Bitmap
        {
            get
            {
                if (_imageWeakReference == null)
                    return null;

                WriteableBitmap image = null;
                _imageWeakReference.TryGetTarget(out image);
                return image;
            }
        }
    }
}
