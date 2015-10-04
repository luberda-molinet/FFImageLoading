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

		public override void SetParameters(object[] parameters)
		{
		}

		public override string Key
		{
			get { return "CircleTransformation"; }
		}

		protected override Bitmap Transform(Bitmap source)
		{
			try
			{
				int size = Math.Min(source.Width, source.Height);
				var transformed = RoundedTransformation.ToRounded(source, size / 2);
				return transformed;
			}
			finally
			{
				source.Recycle();
			}
		}
	}
}

