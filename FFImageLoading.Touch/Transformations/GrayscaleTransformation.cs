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

		public override void SetParameters(object[] parameters)
		{
		}

		public override string Key
		{
			get { return "GrayscaleTransformation"; }
		}

		protected override UIImage Transform(UIImage source)
		{
			try
			{
				using (var colorSpace = CGColorSpace.CreateDeviceGray())
				{
					var transformed = ColorSpaceTransformation.ToColorSpace(source, colorSpace);
					return transformed;	
				}
			}
			finally
			{
				source.Dispose();
			}
		}
	}
}

