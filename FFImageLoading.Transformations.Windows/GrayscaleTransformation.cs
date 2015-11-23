using Windows.UI.Xaml.Media.Imaging;
using FFImageLoading.Transformations.WritableBitmapEx;

namespace FFImageLoading.Transformations
{
    public class GrayscaleTransformation : TransformationBase
    {
        public GrayscaleTransformation()
        {
        }

        public override string Key
        {
            get { return "GrayscaleTransformation"; }
        }

        protected override WriteableBitmap Transform(WriteableBitmap source)
        {
            ToGrayscale(source);

            return source;
        }

        public static void ToGrayscale(WriteableBitmap bmp)
        {
            using (var context = bmp.GetBitmapContext())
            {
                var nWidth = context.Width;
                var nHeight = context.Height;
                var px = context.Pixels;

                var rp = context.Pixels;
                var len = context.Length;
                for (var i = 0; i < len; i++)
                {
                    var c = px[i];
                    var a = (c >> 24) & 0x000000FF;
                    var r = (c >> 16) & 0x000000FF;
                    var g = (c >> 8) & 0x000000FF;
                    var b = (c) & 0x000000FF;

                    // Convert to gray with constant factors 0.2126, 0.7152, 0.0722
                    var gray = (r * 6966 + g * 23436 + b * 2366) >> 15;
                    r = g = b = gray;

                    rp[i] = (a << 24) | (r << 16) | (g << 8) | b;
                }
            }
        }
    }
}
