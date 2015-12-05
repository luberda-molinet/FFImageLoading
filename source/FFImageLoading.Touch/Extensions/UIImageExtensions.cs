using System;
using UIKit;
using CoreGraphics;

namespace FFImageLoading.Extensions
{
    public static class UIImageExtensions
    {
        public static nuint GetMemorySize(this UIImage image)
        {
            return (nuint)(image.CGImage.BytesPerRow * image.CGImage.Height);
        }

		public static UIImage ResizeUIImage(this UIImage image, double desiredWidth, double desiredHeight)
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

			UIGraphics.BeginImageContextWithOptions(newSize, false, (nfloat)1.0);

			try
			{
				image.Draw(new CGRect((nfloat)0.0, (nfloat)0.0, newSize.Width, newSize.Height));

				using (var context = UIGraphics.GetCurrentContext())
				{
					var resizedImage = UIGraphics.GetImageFromCurrentImageContext();

					return resizedImage;
				}	
			}
			finally
			{
				UIGraphics.EndImageContext();
				image.Dispose();
			}
		}
    }
}

