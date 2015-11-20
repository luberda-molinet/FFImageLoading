using System;
using Android.Graphics;

namespace FFImageLoading.Transformations
{
	public class CropTransformation : TransformationBase
	{
		private double _zoomFactor;
		private double _xOffset;
		private double _yOffset;
		private double _cropWidthRatio;
		private double _cropHeightRatio;

		public CropTransformation(double zoomFactor, double xOffset, double yOffset)
		{
			_zoomFactor = zoomFactor;
			_xOffset = xOffset;
			_yOffset = yOffset;
			_cropWidthRatio = 1f;
			_cropHeightRatio = 1f;
		}

		public CropTransformation(double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
		{
			_zoomFactor = zoomFactor;
			_xOffset = xOffset;
			_yOffset = yOffset;
			_cropWidthRatio = cropWidthRatio;
			_cropHeightRatio = cropHeightRatio;
		}

		public override string Key
		{
			get { return string.Format("CropTransformation, zoomFactor = {0}, xOffset = {1}, yOffset = {2}, cropWidthRatio = {3}, cropHeightRatio = {4}", 
				_zoomFactor, _xOffset, _yOffset, _cropWidthRatio, _cropHeightRatio); }
		}

		protected override Bitmap Transform(Bitmap source)
		{
			return ToCropped(source, _zoomFactor, _xOffset, _yOffset, _cropWidthRatio, _cropHeightRatio);
		}

		public static Bitmap ToCropped(Bitmap source, double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
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

			desiredWidth = zoomFactor * desiredWidth;
			desiredHeight = zoomFactor * desiredHeight;

			float cropX = (float)(((sourceWidth - desiredWidth) / 2) + xOffset);
			float cropY = (float)(((sourceHeight - desiredHeight) / 2) + yOffset);

			Bitmap bitmap = Bitmap.CreateBitmap((int)desiredWidth, (int)desiredHeight, Bitmap.Config.Argb8888);

			using (Canvas canvas = new Canvas(bitmap))
			using (Paint paint = new Paint())
			using (BitmapShader shader = new BitmapShader(source, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
			using (Matrix matrix = new Matrix())
			{
				matrix.SetTranslate(-cropX, -cropY);
				shader.SetLocalMatrix(matrix);

				paint.SetShader(shader);
				paint.AntiAlias = true;

				return bitmap;				
			}
		}
	}
}

