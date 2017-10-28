using UIKit;
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

        protected override UIImage Transform(UIImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToSepia(sourceBitmap);
        }

        public static UIImage ToSepia(UIImage source)
        {
            using (var effect = new CISepiaTone() { Image = source.CGImage })
            using (var output = effect.OutputImage)
            using (var context = CIContext.FromOptions(null))
            using (var cgimage = context.CreateCGImage(output, output.Extent))
            {
                return UIImage.FromImage(cgimage);
            }
        }
    }
}

