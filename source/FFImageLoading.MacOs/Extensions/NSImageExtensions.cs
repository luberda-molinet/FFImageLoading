using System;
using AppKit;
using CoreGraphics;
using FFImageLoading.Work;

namespace FFImageLoading.Extensions
{
    public static class NSImageExtensions
    {
        public static nuint GetMemorySize(this NSImage image)
        {
            return (nuint)(image.CGImage.BytesPerRow * image.CGImage.Height);
        }

		public static NSImage ResizeNSImage(this NSImage image, double desiredWidth, double desiredHeight, InterpolationMode interpolationMode)
		{
			double widthRatio = desiredWidth / image.Size.Width;
			double heightRatio = desiredHeight / image.Size.Height;

			double scaleRatio = Math.Min(widthRatio, heightRatio);

			if (desiredWidth == 0)
				scaleRatio = heightRatio;

			if (desiredHeight == 0)
				scaleRatio = widthRatio;

			double aspectWidth = image.Size.Width * scaleRatio;
			double aspectHeight = image.Size.Height * scaleRatio;

			var newSize = new CGSize(aspectWidth, aspectHeight);

            var resizedImage = new NSImage(newSize);
            resizedImage.LockFocus();
            image.Draw(new CGRect(CGPoint.Empty, newSize), CGRect.Empty, NSCompositingOperation.SourceOver, 1.0f);
            resizedImage.UnlockFocus();
            return resizedImage;
		
		}
    }
}

