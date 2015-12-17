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
			using (var colorSpace = CGColorSpace.CreateDeviceGray())
			{
				return ColorSpaceTransformation.ToColorSpace(source, colorSpace);
			}
		}
	}
}

