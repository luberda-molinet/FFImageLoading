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
			try
			{
				double size = Math.Min(source.Size.Width, source.Size.Height);

				var transformed = RoundedTransformation.ToRounded(source, (nfloat)(size / 2));
				return transformed;
			}
			finally
			{
				source.Dispose();
			}
		}
	}
}

