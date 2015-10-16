using System;
using Android.Graphics;

namespace FFImageLoading.Transformations
{
	public class RoundedTransformation : TransformationBase
	{
		private double _radius;
		private double _cropWidthRatio;
		private double _cropHeightRatio;

		public RoundedTransformation(double radius)
		{
			_radius = radius;
			_cropWidthRatio = 1f;
			_cropHeightRatio = 1f;
		}

		public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio)
		{
			_radius = radius;
			_cropWidthRatio = cropWidthRatio;
			_cropHeightRatio = cropHeightRatio;
		}

		public override string Key
		{
			get { return string.Format("RoundedTransformation, radius = {0}, cropWidthRatio = {1}, cropHeightRatio = {2}", 
				_radius, _cropWidthRatio, _cropHeightRatio); }
		}
			
		protected override Bitmap Transform(Bitmap source)
		{
			return ToRounded(source, (float)_radius, _cropWidthRatio, _cropHeightRatio);
		}

		public static Bitmap ToRounded(Bitmap source, float rad, double cropWidthRatio, double cropHeightRatio)
		{
			double sourceWidth = source.Width;
			double sourceHeight = source.Height;

			double desiredWidth = sourceWidth;
			double desiredHeight = sourceHeight;

			double desiredRatio = cropWidthRatio / cropHeightRatio;
			double currentRatio = sourceWidth / sourceHeight;

			if (currentRatio > desiredRatio)
				desiredWidth = (cropWidthRatio * sourceHeight / cropHeightRatio);
			else if (currentRatio < desiredRatio)
				desiredHeight = (cropHeightRatio * sourceWidth / cropWidthRatio);

			float cropX = (float)((sourceWidth - desiredWidth) / 2);
			float cropY = (float)((sourceHeight - desiredHeight) / 2);

			if (rad == 0)
				rad = (float)(Math.Min(desiredWidth, desiredHeight) / 2);

			Bitmap bitmap = Bitmap.CreateBitmap((int)desiredWidth, (int)desiredHeight, Bitmap.Config.Argb8888);

			using (Canvas canvas = new Canvas(bitmap))
			using (Paint paint = new Paint())
			using (BitmapShader shader = new BitmapShader(source, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
			using (Matrix matrix = new Matrix())
			{
				if (cropX != 0 || cropY != 0)
				{
					matrix.SetTranslate(-cropX, -cropY);
					shader.SetLocalMatrix(matrix);
				}

				paint.SetShader(shader);
				paint.AntiAlias = true;

				RectF rectF = new RectF(0, 0, (int)desiredWidth, (int)desiredHeight);
				canvas.DrawRoundRect(rectF, rad, rad, paint);

				return bitmap;				
			}
		}
	}
}

