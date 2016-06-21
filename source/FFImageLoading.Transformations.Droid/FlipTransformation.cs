using System;
using Android.Graphics;
using Android.Runtime;
using Android.Util;

namespace FFImageLoading.Transformations
{
	[Preserve(AllMembers = true)]
	public class FlipTransformation: TransformationBase
	{
		public FlipTransformation(FlipType flipType)
		{
			FlipType = flipType;
		}

		public override string Key
		{
			get { return string.Format("FlipTransformation,Type={0}", FlipType); }
		}

		public FlipType FlipType { get; set; }

		protected override Bitmap Transform(Bitmap source)
		{
			switch (FlipType)
			{
				case FlipType.Vertical:
					return Flip(source, 1, -1);

				case FlipType.Horizontal:
					return Flip(source, -1, 1);

				default:
					throw new Exception("Invalid FlipType");
			}
		}

		private Bitmap Flip(Bitmap source, int sx, int sy)
		{
			using (Matrix matrix = new Matrix())
			{
				matrix.PreScale(sx, sy);
				Bitmap output = Bitmap.CreateBitmap(source, 0, 0, source.Width, source.Height, matrix, false);
				output.Density = (int)DisplayMetricsDensity.Default;
				return output;
			}
		}
	}
}