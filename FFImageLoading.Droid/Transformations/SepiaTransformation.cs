using System;
using FFImageLoading.Work;
using Android.Graphics;

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

		protected override Bitmap Transform(Bitmap source)
		{
			try
			{
				var transformed = ToSepia(source);
				return transformed;
			}
			finally
			{
				source.Recycle();
			}
		}

		public static Bitmap ToSepia(Bitmap source)
		{
			using (ColorMatrix saturation = new ColorMatrix())
			using (ColorMatrix rgbFilter = new ColorMatrix())
			{
				saturation.SetSaturation(0f);
				rgbFilter.SetScale(1.0f, 0.95f, 0.82f, 1.0f);
				saturation.SetConcat(rgbFilter, saturation);
				return ColorSpaceTransformation.ToColorSpace(source, saturation);
			}
		}
	}
}

