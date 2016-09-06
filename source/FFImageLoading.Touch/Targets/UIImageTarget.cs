using System;
using FFImageLoading.Work;
using UIKit;

namespace FFImageLoading
{
    public class UIImageTarget: Target<UIImage, ImageLoaderTask>
    {
        private WeakReference<UIImage> _imageWeakReference = null;

        public override void Set(ImageLoaderTask task, UIImage image, bool isLocalOrFromCache, bool isLoadingPlaceholder)
        {
            if (task.IsCancelled)
                return;

            if (!isLoadingPlaceholder)
            {
                if (_imageWeakReference == null)
                    _imageWeakReference = new WeakReference<UIImage>(image);
                else
                    _imageWeakReference.SetTarget(image);
            }
        }

        public UIImage UIImage
        {
            get
            {
                if (_imageWeakReference == null)
                    return null;

                UIImage image = null;
                _imageWeakReference.TryGetTarget(out image);
                return image;
            }
        }
    }
}

