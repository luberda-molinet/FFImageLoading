using CoreGraphics;
using Foundation;
using UIKit;

namespace FFImageLoading.Transformations
{
	[Preserve(AllMembers = true)]
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

