using UIKit;
using CoreGraphics;
using CoreImage;
using Foundation;

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

        protected override UIImage Transform(UIImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToBlurred(sourceBitmap, (float)Radius);
        }

        public static UIImage ToBlurred(UIImage source, float rad)
        {
            using (var context = CIContext.FromOptions(new CIContextOptions { UseSoftwareRenderer = false }))
            using (var inputImage = CIImage.FromCGImage(source.CGImage))
            using (var filter = new CIGaussianBlur() { Image = inputImage, Radius = rad })
            using (var resultImage = context.CreateCGImage(filter.OutputImage, inputImage.Extent))
            {
                return new UIImage(resultImage);
            }
        }
    }
}

