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

		protected override UIImage Transform(UIImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
		{
			switch (FlipType)
			{
				case FlipType.Vertical:
					return new UIImage(sourceBitmap.CGImage, sourceBitmap.CurrentScale, UIImageOrientation.DownMirrored);

				case FlipType.Horizontal:
					return new UIImage(sourceBitmap.CGImage, sourceBitmap.CurrentScale, UIImageOrientation.UpMirrored);

				default:
					throw new Exception("Invalid FlipType");
			}
		}
	}
}

