using System;
using FFImageLoading.Work;
using Android.Graphics;

namespace FFImageLoading.Transformations
{
	public class GrayscaleTransformation : TransformationBase, IMultiplatformTransformation
	{
		public GrayscaleTransformation()
		{
		}

		public void SetParameters(object[] parameters)
		{
		}

		public override string Key
		{
			get { return "GrayscaleTransformation"; }
		}

		protected override Bitmap Transform(Bitmap source)
		{
			var transformed = ToGrayscale(source);
			source.Recycle();

			return transformed;
		}

		public static Bitmap ToGrayscale(Bitmap source)
		{
			int width = source.Width;
			int height = source.Height;

			Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);

			using (Canvas canvas = new Canvas(bitmap))
			using (ColorMatrix saturation = new ColorMatrix())
			using (Paint paint = new Paint())
			{
				saturation.SetSaturation(0f);
				paint.SetColorFilter(new ColorMatrixColorFilter(saturation));
				canvas.DrawBitmap(source, 0, 0, paint);

				return bitmap;	
			}
		}
	}
}

