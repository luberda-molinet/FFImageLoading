using System;
using FFImageLoading.Work;
using UIKit;
using CoreGraphics;
using CoreImage;

namespace FFImageLoading.Transformations
{
	public class ColorSpaceTransformation: TransformationBase
	{
		CGColorSpace _colorSpace;

		public ColorSpaceTransformation(CGColorSpace colorSpace)
		{
			_colorSpace = colorSpace;
		}

		public override void SetParameters(object[] parameters)
		{
		}

		public override string Key
		{
			get { return "ColorSpaceTransformation"; }
		}

		protected override UIImage Transform(UIImage source)
		{
			try
			{
				var transformed = ToColorSpace(source, _colorSpace);
				return transformed;
			}
			finally
			{
				source.Dispose();
			}
		}

		public static UIImage ToColorSpace(UIImage source, CGColorSpace colorSpace)
		{
			CGRect bounds = new CGRect(0, 0, source.Size.Width, source.Size.Height);

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

