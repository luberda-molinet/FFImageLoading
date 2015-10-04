using System;
using FFImageLoading.Work;
using UIKit;
using CoreGraphics;

namespace FFImageLoading.Transformations
{
	public class CircleTransformation : TransformationBase
	{
		public CircleTransformation()
		{
		}

		public override void SetParameters(object[] parameters)
		{
		}

		public override string Key
		{
			get { return "CircleTransformation"; }
		}

		protected override UIImage Transform(UIImage source)
		{
			UIGraphics.BeginImageContextWithOptions(source.Size, false, (nfloat)0.0);
			try
			{
				var bounds = new CGRect(0, 0, source.Size.Width, source.Size.Height);
				using (var path = UIBezierPath.FromOval(bounds))			
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
				source.Dispose();
			}
		}
	}
}

