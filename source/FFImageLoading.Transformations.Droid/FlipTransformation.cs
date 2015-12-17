using System;
using Android.Graphics;
using Android.Util;

namespace FFImageLoading.Transformations
{
	public class FlipTransformation: TransformationBase
	{
		private FlipType _flipType;

		public FlipTransformation(FlipType flipType)
		{
			_flipType = flipType;
		}

		public override string Key
		{
			get { return string.Format("FlipTransformation,Type={0}", _flipType); }
		}

		protected override Bitmap Transform(Bitmap source)
		{
			switch (_flipType)
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