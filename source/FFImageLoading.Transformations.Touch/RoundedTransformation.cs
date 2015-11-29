using System;
using UIKit;
using CoreGraphics;

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

		protected override UIImage Transform(UIImage source)
		{
			return ToRounded(source, (nfloat)_radius, _cropWidthRatio, _cropHeightRatio);
		}

		public static UIImage ToRounded(UIImage source, nfloat rad, double cropWidthRatio, double cropHeightRatio)
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

			float cropX = (float)((sourceWidth - desiredWidth) / 2);
			float cropY = (float)((sourceHeight - desiredHeight) / 2);

			if (rad == 0)
				rad = (nfloat)(Math.Min(desiredWidth, desiredHeight) / 2);

			UIGraphics.BeginImageContextWithOptions(new CGSize(desiredWidth, desiredHeight), false, (nfloat)0.0);

			try
			{
				using (var context = UIGraphics.GetCurrentContext())
				{
					var clippedRect = new CGRect(0, 0, desiredWidth, desiredHeight);
					context.BeginPath();

					using (var path = UIBezierPath.FromRoundedRect(clippedRect, rad))
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

