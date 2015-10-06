using System;
using FFImageLoading.Work;
using Android.Graphics;

namespace FFImageLoading.Transformations
{
	public class ColorSpaceTransformation : TransformationBase
	{
		ColorMatrix _colorMatrix;

		public ColorSpaceTransformation(ColorMatrix colorMatrix)
		{
			_colorMatrix = colorMatrix;
		}

		public override void SetParameters(object[] parameters)
		{
			throw new NotImplementedException();
		}

		public override string Key
		{
			get { return "ColorSpaceTransformation"; }
		}

		protected override Bitmap Transform(Bitmap source)
		{
			try
			{
				var transformed = ToColorSpace(source, _colorMatrix);
				return transformed;
			}
			finally
			{
				source.Recycle();
			}
		}

		public static Bitmap ToColorSpace(Bitmap source, ColorMatrix colorMatrix)
		{
			int width = source.Width;
			int height = source.Height;

			Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);

			using (Canvas canvas = new Canvas(bitmap))
			using (Paint paint = new Paint())
			{
				paint.SetColorFilter(new ColorMatrixColorFilter(colorMatrix));
				canvas.DrawBitmap(source, 0, 0, paint);

				return bitmap;	
			}
		}
	}
}

