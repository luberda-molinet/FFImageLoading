using System;
using Foundation;
using AppKit;

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

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            switch (FlipType)
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

