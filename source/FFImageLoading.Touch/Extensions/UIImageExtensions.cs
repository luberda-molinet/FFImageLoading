using System;
using UIKit;
using CoreGraphics;
using FFImageLoading.Work;

namespace FFImageLoading.Extensions
{
    public static class UIImageExtensions
    {
        public static nuint GetMemorySize(this UIImage image)
        {
            return (nuint)(image.CGImage.BytesPerRow * image.CGImage.Height);
        }

		public static UIImage ResizeUIImage(this UIImage image, double desiredWidth, double desiredHeight, InterpolationMode interpolationMode)
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
				image.Dispose();
			}
		}
    }
}

