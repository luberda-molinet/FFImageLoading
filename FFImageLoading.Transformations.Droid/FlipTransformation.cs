using System;
using FFImageLoading.Work;
using Android.Graphics;

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
			get { return string.Format("FlipTransformation, Type=", _flipType.ToString()); }
		}

		protected override Bitmap Transform(Bitmap source)
		{
			switch (_flipType)
			{
				case FlipType.Vertical:
					throw new NotImplementedException();

				case FlipType.Horizontal:
					throw new NotImplementedException();

				default:
					throw new Exception("Invalid FlipType");
			}
		}
	}
}