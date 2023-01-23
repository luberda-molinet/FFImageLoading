using System;
using CoreGraphics;
using FFImageLoading.Work;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif

namespace FFImageLoading.Extensions
{
    public static class PImageExtensions
    {
        public static nuint GetMemorySize(this PImage image)
        {
            return (nuint)(image.CGImage.BytesPerRow * image.CGImage.Height);
        }

        public static PImage ResizeUIImage(this PImage image, double desiredWidth, double desiredHeight, InterpolationMode interpolationMode)
        {
            var widthRatio = desiredWidth / image.Size.Width;
            var heightRatio = desiredHeight / image.Size.Height;
            var scaleRatio = Math.Min(widthRatio, heightRatio);

            if (Math.Abs(desiredWidth) < double.Epsilon )
                scaleRatio = heightRatio;

            if (Math.Abs(desiredHeight) < double.Epsilon)
                scaleRatio = widthRatio;

            var aspectWidth = image.Size.Width * scaleRatio;
            var aspectHeight = image.Size.Height * scaleRatio;

            var newSize = new CGSize(aspectWidth, aspectHeight);
#if __MACOS__
            var resizedImage = new PImage(newSize);
            resizedImage.LockFocus();
            image.Draw(new CGRect(CGPoint.Empty, newSize), CGRect.Empty, NSCompositingOperation.SourceOver, 1.0f);
            resizedImage.UnlockFocus();
            return resizedImage;
#elif __IOS__
            UIGraphics.BeginImageContextWithOptions(newSize, false, 0);

            try
            {
                image.Draw(new CGRect((nfloat)0.0, (nfloat)0.0, newSize.Width, newSize.Height));

                using (var context = UIGraphics.GetCurrentContext())
                {
                    if (interpolationMode == InterpolationMode.None)
                        context.InterpolationQuality = CGInterpolationQuality.None;
                    else if (interpolationMode == InterpolationMode.Low)
                        context.InterpolationQuality = CGInterpolationQuality.Low;
                    else if (interpolationMode == InterpolationMode.Medium)
                        context.InterpolationQuality = CGInterpolationQuality.Medium;
                    else if (interpolationMode == InterpolationMode.High)
                        context.InterpolationQuality = CGInterpolationQuality.High;
                    else
                        context.InterpolationQuality = CGInterpolationQuality.Low;

                    var resizedImage = UIGraphics.GetImageFromCurrentImageContext();

                    return resizedImage;
                }
            }
            finally
            {
                UIGraphics.EndImageContext();
                image.TryDispose();
            }
#endif
        }

        public static System.IO.Stream AsPngStream(this PImage image)
        {
#if __IOS__
            return image.AsPNG()?.AsStream();
#elif __MACOS__
            var imageRep = new NSBitmapImageRep(image.AsTiff());
            return imageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png)
                                                             .AsStream();
#endif
        }

        public static System.IO.Stream AsJpegStream(this PImage image, int quality = 80)
        {
#if __IOS__
            return image.AsJPEG(quality / 100f).AsStream();
#elif __MACOS__
            // todo: jpeg quality?
            var imageRep = new NSBitmapImageRep(image.AsTiff());
            return imageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg)
                           .AsStream();
#endif
        }
    }
}

