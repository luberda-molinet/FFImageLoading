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

		public override string Key
		{
			get { return "SepiaTransformation"; }
		}

		protected override UIImage Transform(UIImage source)
		{
			return ToSepia(source);
		}

		public static UIImage ToSepia(UIImage source)
		{
			using (var filter =  new CISepiaTone() { Intensity = 0.8f })
			{
				return ColorSpaceTransformation.ToFilter(source, filter);
			}
		}
	}
}

