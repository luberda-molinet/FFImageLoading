using System;
using FFImageLoading.Work;
using CoreGraphics;
using UIKit;

namespace FFImageLoading.Transformations
{
	public class GrayscaleTransformation : TransformationBase
	{
		public GrayscaleTransformation()
		{
		}

		public override string Key
		{
			get { return "GrayscaleTransformation"; }
		}

		protected override UIImage Transform(UIImage source)
		{
			var transformed = ToGrayscale(source);
			source.Dispose();

			return transformed;
		}

		public static UIImage ToGrayscale(UIImage source)
		{
			CGRect bounds = new CGRect(0, 0, source.Size.Width, source.Size.Height);

			using (var colorSpace = CGColorSpace.CreateDeviceGray())
			using (var context = new CGBitmapContext(IntPtr.Zero, (int)bounds.Width, (int)bounds.Height, 8, 0, colorSpace, CGImageAlphaInfo.None)) 
			{
				context.DrawImage(bounds, source.CGImage);
				using (var imageRef = context.ToImage())
				{
					return new UIImage(imageRef);
				}
			}
		}
	}
}

