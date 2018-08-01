using AppKit;
using CoreImage;
using Foundation;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class SepiaTransformation : TransformationBase
    {
        public SepiaTransformation()
        {
        }

        public override string Key
        {
            get { return "SepiaTransformation"; }
        }

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() => ToSepia(sourceBitmap));
        }

        public static NSImage ToSepia(NSImage source)
        {
            using (var inputImage = CIImage.FromCGImage(source.CGImage))
            using (var filter = new CISepiaTone() { Image = inputImage })
            using (var resultImage = new NSCIImageRep(filter.OutputImage))
            {
                var nsImage = new NSImage(resultImage.Size);
                nsImage.AddRepresentation(resultImage);
                return nsImage;
            }
        }
    }
}

