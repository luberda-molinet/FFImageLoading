using System;
using FFImageLoading.Work;
using UIKit;
using CoreGraphics;

namespace FFImageLoading.Transformations
{
	public class RoundedTransformation : TransformationBase
	{
		private double _radius;

		public RoundedTransformation(double radius)
		{
			_radius = radius;
		}

		public override void SetParameters(object[] parameters)
		{
			_radius = (double)parameters[0];
		}

		public override string Key
		{
			get { return string.Format("RoundedTransformation, radius = {0}", _radius); }
		}

		protected override UIImage Transform(UIImage source)
		{
			return ToRounded(source, (nfloat)_radius);
		}

		public static UIImage ToRounded(UIImage source, nfloat rad)
		{
			UIGraphics.BeginImageContextWithOptions(source.Size, false, (nfloat)0.0);

			nfloat size = (nfloat)Math.Min(source.Size.Width, source.Size.Height);

			try
			{
				CGRect bounds = new CGRect(0, 0, size, size);

				using (var path = UIBezierPath.FromRoundedRect(bounds, rad))			
				{
					path.AddClip();
					source.Draw(bounds);
					var newImage = UIGraphics.GetImageFromCurrentImageContext();
					return newImage;
				}
			}
			finally
			{
				UIGraphics.EndImageContext();
			}
		}
	}
}

