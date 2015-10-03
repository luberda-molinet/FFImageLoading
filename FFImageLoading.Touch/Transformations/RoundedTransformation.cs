using System;
using FFImageLoading.Work;
using UIKit;
using CoreGraphics;

namespace FFImageLoading.Transformations
{
	public class RoundedTransformation : TransformationBase, IMultiplatformTransformation
	{
		double radius;

		public RoundedTransformation(double radius)
		{
			this.radius = radius;
		}

		public void SetParameters(object[] parameters)
		{
			this.radius = (double)parameters[0];
		}

		public override string Key
		{
			get { return string.Format("RoundedTransformation, radius = {0}", radius); }
		}

		protected override UIImage Transform(UIImage source)
		{
			var transformed = ToRounded(source, (nfloat)radius);
			source.Dispose();

			return transformed;
		}

		public static UIImage ToRounded(UIImage source, nfloat rad)
		{
			UIGraphics.BeginImageContextWithOptions(source.Size, false, (nfloat)0.0);
			CGRect bounds = new CGRect(0, 0, source.Size.Width, source.Size.Height);

			using (var path = UIBezierPath.FromRoundedRect(bounds, rad))			
			{
				path.AddClip();
				source.Draw(bounds);
				var newImage = UIGraphics.GetImageFromCurrentImageContext();
				UIGraphics.EndImageContext();

				return newImage;
			}
		}
	}
}

