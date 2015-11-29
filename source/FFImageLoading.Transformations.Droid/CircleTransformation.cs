using System;
using FFImageLoading.Work;
using Android.Graphics;

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

		protected override Bitmap Transform(Bitmap source)
		{
			return RoundedTransformation.ToRounded(source, 0f, 1f, 1f);
		}
	}
}

