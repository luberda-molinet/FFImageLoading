using System;
using UIKit;

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

		protected override UIImage Transform(UIImage source)
		{
			switch (_flipType)
			{
				case FlipType.Vertical:
					return new UIImage(source.CGImage, source.CurrentScale, UIImageOrientation.DownMirrored);

				case FlipType.Horizontal:
					return new UIImage(source.CGImage, source.CurrentScale, UIImageOrientation.UpMirrored);

				default:
					throw new Exception("Invalid FlipType");
			}
		}
	}
}

