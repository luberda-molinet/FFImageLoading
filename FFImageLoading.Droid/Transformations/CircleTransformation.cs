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
			int size = Math.Min(source.Width, source.Height);
			return RoundedTransformation.ToRounded(source, size / 2);
		}
	}
}

