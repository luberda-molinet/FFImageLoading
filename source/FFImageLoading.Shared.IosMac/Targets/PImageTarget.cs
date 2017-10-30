using System;
using FFImageLoading.Work;
#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif


namespace FFImageLoading.Targets
{
    public class PImageTarget: Target<PImage, PImage>
    {
        private WeakReference<PImage> _imageWeakReference = null;

        public override void Set(IImageLoaderTask task, PImage image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            if (_imageWeakReference == null)
                _imageWeakReference = new WeakReference<PImage>(image);
            else
                _imageWeakReference.SetTarget(image);
        }

        public PImage PImage
        {
            get
            {
                if (_imageWeakReference == null)
                    return null;

                PImage image = null;
                _imageWeakReference.TryGetTarget(out image);
                return image;
            }
        }
    }
}

