using CoreGraphics;
using CoreImage;
using Foundation;
using AppKit;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class BlurredTransformation : TransformationBase
    {
        public BlurredTransformation()
        {
            Radius = 20d;
        }

        public BlurredTransformation(double radius)
        {
            Radius = radius;
        }

        public double Radius { get; set; }

        public override string Key
        {
            get { return string.Format("BlurredTransformation,radius={0}", Radius); }
        }

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() => ToBlurred(sourceBitmap, (float)Radius));
        }

        public static NSImage ToBlurred(NSImage source, float rad)
        {
            using (var inputImage = CIImage.FromCGImage(source.CGImage))
            using (var filter = new CIGaussianBlur() { Image = inputImage, Radius = rad })
            using (var resultImage = new NSCIImageRep(filter.OutputImage))
            {
                var nsImage = new NSImage(resultImage.Size);
                nsImage.AddRepresentation(resultImage);
                return nsImage;
            }
        }
    }
}

