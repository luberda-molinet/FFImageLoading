using System;
using FFImageLoading.Work;
using UIKit;

namespace FFImageLoading.Transformations
{
	public class CircleTransformation : TransformationBase, IMultiplatformTransformation
	{
		public CircleTransformation()
		{
		}

		public void SetParameters(object[] parameters)
		{
		}

		public override string Key
		{
			get { return "CircleTransformation"; }
		}

		protected override UIImage Transform(UIImage source)
		{
			double size = Math.Min(source.Size.Width, source.Size.Height);
			var radius = size / 2;
			var transformed = RoundedTransformation.ToRounded(source, (nfloat)radius);
			source.Dispose();

			return transformed;
		}
	}
}

