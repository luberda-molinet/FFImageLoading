using System;
using FFImageLoading.Work;
using UIKit;
using CoreGraphics;
using CoreImage;

namespace FFImageLoading.Transformations
{
	public class SepiaTransformation : TransformationBase
	{
		public SepiaTransformation()
		{
		}

		public override void SetParameters(object[] parameters)
		{
		}

		public override string Key
		{
			get { return "SepiaTransformation"; }
		}

		protected override UIImage Transform(UIImage source)
		{
			try
			{
				var transformed = ToSepia(source);
				return transformed;
			}
			finally
			{
				source.Dispose();
			}
		}

		public static UIImage ToSepia(UIImage source)
		{
			using (var context = CIContext.FromOptions(new CIContextOptions { UseSoftwareRenderer = false }))
			using (var inputImage = CIImage.FromCGImage(source.CGImage))
			using (var filter =  new CISepiaTone() { Image = inputImage, Intensity = 0.8f })
			using (var resultImage = context.CreateCGImage(filter.OutputImage, inputImage.Extent))
			{
				return new UIImage(resultImage);
			}
		}
	}
}

