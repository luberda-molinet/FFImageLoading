using System;
using UIKit;
using CoreGraphics;
using FFImageLoading.Transformations.Extensions;

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

		protected override UIImage Transform(UIImage source)
		{
			return ToRounded(source, (nfloat)_radius, _cropWidthRatio, _cropHeightRatio, _borderSize, _borderHexColor);
		}

		public static UIImage ToRounded(UIImage source, nfloat rad, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
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
			else
				rad = (nfloat)(rad * (desiredWidth + desiredHeight) / 2 / 500);

			UIGraphics.BeginImageContextWithOptions(new CGSize(desiredWidth, desiredHeight), false, (nfloat)0.0);

			try
			{
				using (var context = UIGraphics.GetCurrentContext())
				{
					var clippedRect = new CGRect(0d, 0d, desiredWidth, desiredHeight);

					context.BeginPath();

					using (var path = UIBezierPath.FromRoundedRect(clippedRect, rad))
					{
						context.AddPath(path.CGPath);
						context.Clip();
					}

					var drawRect = new CGRect(-cropX, -cropY, sourceWidth, sourceHeight);
					source.Draw(drawRect);

					if (borderSize > 0d) 
					{
						borderSize = (borderSize * (desiredWidth + desiredHeight) / 2d / 1000d);
						UIColor borderColor = UIColor.Clear;

						try
						{
							borderColor = UIColor.Clear.FromHexString(borderHexColor);
						}
						catch(Exception)
						{
						}

						var borderRect = new CGRect((0d + borderSize/2d), (0d + borderSize/2d), 
							(desiredWidth - borderSize), (desiredHeight - borderSize));

						context.BeginPath();

						using (var path = UIBezierPath.FromRoundedRect(borderRect, rad))
						{
							context.SetStrokeColor(borderColor.CGColor);
							context.SetLineWidth((nfloat)borderSize);
							context.AddPath(path.CGPath);
							context.StrokePath();
						}
					}

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

