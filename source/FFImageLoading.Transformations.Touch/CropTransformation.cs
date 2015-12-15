using System;
using UIKit;
using CoreGraphics;

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

		protected override UIImage Transform(UIImage source)
		{
			return ToCropped(source, _zoomFactor, _xOffset, _yOffset, _cropWidthRatio, _cropHeightRatio);
		}

		public static UIImage ToCropped(UIImage source, double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
		{
			double sourceWidth = source.Size.Width;
			double sourceHeight = source.Size.Height;

			double desiredWidth = sourceWidth;
			double desiredHeight = sourceHeight;

			double desiredRatio = cropWidthRatio / cropHeightRatio;
			double currentRatio = sourceWidth / sourceHeight;

			if (currentRatio > desiredRatio)
				desiredWidth = (cropWidthRatio * sourceHeight / cropHeightRatio);
			else if (currentRatio < desiredRatio)
				desiredHeight = (cropHeightRatio * sourceWidth / cropWidthRatio);

			if (zoomFactor < 1f)
				zoomFactor = 1f;

			xOffset = xOffset * desiredWidth;
			yOffset = yOffset * desiredHeight;

			desiredWidth =  desiredWidth / zoomFactor;
			desiredHeight = desiredHeight / zoomFactor;

			float cropX = (float)(((sourceWidth - desiredWidth) / 2) + xOffset);
			float cropY = (float)(((sourceHeight - desiredHeight) / 2) + yOffset);

			if (cropX < 0)
				cropX = 0;

			if (cropY < 0)
				cropY = 0;

			if (cropX + desiredWidth > sourceWidth)
				cropX = (float)(sourceWidth - desiredWidth);

			if (cropY + desiredHeight > sourceHeight)
				cropY = (float)(sourceHeight - desiredHeight);

			UIGraphics.BeginImageContextWithOptions(new CGSize(desiredWidth, desiredHeight), false, (nfloat)0.0);

			try
			{
				using (var context = UIGraphics.GetCurrentContext())
				{
					var clippedRect = new CGRect(0, 0, desiredWidth, desiredHeight);
					context.BeginPath();

					using (var path = UIBezierPath.FromRect(clippedRect))
					{
						context.AddPath(path.CGPath);
						context.Clip();
					}

					var drawRect = new CGRect(-cropX, -cropY, sourceWidth, sourceHeight);
					source.Draw(drawRect);
					var modifiedImage = UIGraphics.GetImageFromCurrentImageContext();

					return modifiedImage;
				}
			}
			finally
			{
				UIGraphics.EndImageContext();
			}
		}
	}
}

