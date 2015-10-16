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

		public override string Key
		{
			get { return "CircleTransformation"; }
		}

		protected override UIImage Transform(UIImage source)
		{
			return RoundedTransformation.ToRounded(source, 0f, 1f, 1f);
		}
	}
}

