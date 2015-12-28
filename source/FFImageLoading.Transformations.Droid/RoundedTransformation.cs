using System;
using Android.Graphics;

namespace FFImageLoading.Transformations
{
	public class RoundedTransformation : TransformationBase
	{
		private double _radius;
		private double _cropWidthRatio;
		private double _cropHeightRatio;

		private double _borderSize;
		private string _borderHexColor;

		public RoundedTransformation(double radius) : this(radius, 1d, 1d)
		{
		}

		public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio) : this(radius, cropWidthRatio, cropHeightRatio, 0d, null)
		{
		}

		public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
		{
			_radius = radius;
			_cropWidthRatio = cropWidthRatio;
			_cropHeightRatio = cropHeightRatio;
			_borderSize = borderSize;
			_borderHexColor = borderHexColor;
		}

		public override string Key
		{
			get { return string.Format("RoundedTransformation,radius={0},cropWidthRatio={1},cropHeightRatio={2},borderSize={3},borderHexColor={4}", 
				_radius, _cropWidthRatio, _cropHeightRatio, _borderSize, _borderHexColor); }
		}
			
		protected override Bitmap Transform(Bitmap source)
		{
			return ToRounded(source, (float)_radius, _cropWidthRatio, _cropHeightRatio, _borderSize, _borderHexColor);
		}

		public static Bitmap ToRounded(Bitmap source, float rad, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
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

			float cropX = (float)((sourceWidth - desiredWidth) / 2d);
			float cropY = (float)((sourceHeight - desiredHeight) / 2d);

			if (rad == 0)
				rad = (float)(Math.Min(desiredWidth, desiredHeight) / 2d);
			else
				rad = (float)(rad * (desiredWidth + desiredHeight) / 2d / 500d);

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

				RectF rectF = new RectF(0f, 0f, (float)desiredWidth, (float)desiredHeight);
				canvas.DrawRoundRect(rectF, rad, rad, paint);

				if (borderSize > 0d) 
				{
					borderSize = (borderSize * (desiredWidth + desiredHeight) / 2d / 500d);
					Color borderColor = Color.Transparent;

					try
					{
						if(!borderHexColor.StartsWith("#", StringComparison.Ordinal))
							borderHexColor.Insert(0, "#");
						borderColor = Color.ParseColor(borderHexColor);
					}
					catch(Exception)
					{
					}

					paint.Color = borderColor;
					paint.SetStyle(Paint.Style.Stroke);
					paint.StrokeWidth = (float)borderSize;
					paint.SetShader(null);

					RectF borderRectF = new RectF((float)(0d + borderSize/2d), (float)(0d + borderSize/2d), 
						(float)(desiredWidth - borderSize/2d), (float)(desiredHeight - borderSize/2d));

					canvas.DrawRoundRect(borderRectF, rad, rad, paint);
				}

				return bitmap;				
			}
		}
	}
}

