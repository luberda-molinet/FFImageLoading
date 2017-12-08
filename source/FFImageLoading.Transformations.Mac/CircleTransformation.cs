using Foundation;
using AppKit;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CircleTransformation : TransformationBase
    {
        public CircleTransformation() : this(0d, null)
        {
        }

        public CircleTransformation(double borderSize, string borderHexColor)
        {
            BorderSize = borderSize;
            BorderHexColor = borderHexColor;
        }

        public double BorderSize { get; set; }
        public string BorderHexColor { get; set; }


        public override string Key
        {
            get { return string.Format("CircleTransformation,borderSize={0},borderHexColor={1}", BorderSize, BorderHexColor); }
        }

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() => RoundedTransformation.ToRounded(sourceBitmap, 0f, 1f, 1f, BorderSize, BorderHexColor));
        }
    }
}

