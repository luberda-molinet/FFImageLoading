using System;
using FFImageLoading.Work;
using Android.Graphics;

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

		protected override Bitmap Transform(Bitmap source)
		{
			int size = Math.Min(source.Width, source.Height);
			var transformed = RoundedTransformation.ToRounded(source, size / 2);
			source.Recycle();

			return transformed;
		}
	}
}

