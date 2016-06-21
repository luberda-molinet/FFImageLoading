using System;
using Foundation;
using UIKit;

namespace FFImageLoading.Transformations
{
	[Preserve(AllMembers = true)]
	public class FlipTransformation: TransformationBase
	{
		public FlipTransformation() : this(FlipType.Horizontal)
		{
		}

		public FlipTransformation(FlipType flipType)
		{
			FlipType = flipType;
		}

		public override string Key
		{
			get { return string.Format("FlipTransformation,Type={0}", FlipType); }
		}

		public FlipType FlipType { get; set; }

		protected override UIImage Transform(UIImage source)
		{
			switch (FlipType)
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

