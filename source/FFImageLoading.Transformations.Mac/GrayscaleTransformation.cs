using CoreGraphics;
using CoreImage;
using Foundation;
using AppKit;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class GrayscaleTransformation : TransformationBase
    {
        public GrayscaleTransformation()
        {
        }

        public override string Key
        {
            get { return "GrayscaleTransformation"; }
        }

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() =>
            {
                using (var inputImage = CIImage.FromCGImage(sourceBitmap.CGImage))
                using (var filter = new CIPhotoEffectMono() { Image = inputImage })
                using (var resultImage = new NSCIImageRep(filter.OutputImage))
                {
                    var nsImage = new NSImage(resultImage.Size);
                    nsImage.AddRepresentation(resultImage);
                    return nsImage;
                }
            });
        }
    }
}

