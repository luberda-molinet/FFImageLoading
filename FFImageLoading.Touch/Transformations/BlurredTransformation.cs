using System;
using FFImageLoading.Work;
using UIKit;
using CoreGraphics;
using CoreImage;

namespace FFImageLoading.Transformations
{
	public class BlurredTransformation: TransformationBase
	{
		private double _radius;

		public BlurredTransformation(double radius)
		{
			_radius = radius;
		}

		public override string Key
		{
			get { return string.Format("BlurredTransformation, radius = {0}", _radius); }
		}

		protected override UIImage Transform(UIImage source)
		{
			return ToBlurred(source, (float)_radius);
		}


		public static UIImage ToBlurred(UIImage source, float rad)
		{
			using (var context = CIContext.FromOptions(new CIContextOptions { UseSoftwareRenderer = false }))
			using (var inputImage = CIImage.FromCGImage(source.CGImage))
			using (var filter = new CIGaussianBlur() { Image = inputImage, Radius = rad })
			using (var resultImage = context.CreateCGImage(filter.OutputImage, inputImage.Extent))
			{
				return new UIImage(resultImage);
			}
		}
	}
}

