using System;
using FFImageLoading.Work;
using Android.Graphics;

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

		protected override Bitmap Transform(Bitmap source)
		{
			return ToGrayscale(source);
		}

		public static Bitmap ToGrayscale(Bitmap source)
		{
			using (var colorMatrix = new ColorMatrix())
			{
				colorMatrix.SetSaturation(0f);
				return ColorSpaceTransformation.ToColorSpace(source, colorMatrix);
			}
		}
	}
}

