using FFImageLoading.Work;

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

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            ToGrayscale(bitmapSource);

            return bitmapSource;
        }

        public static void ToGrayscale(BitmapHolder bmp)
        {
            var nWidth = bmp.Width;
            var nHeight = bmp.Height;

            var len = bmp.PixelCount;

            for (var i = 0; i < len; i++)
            {
                var color = bmp.GetPixel(i);
                int a = color.A;
                int r = color.R;
                int g = color.G;
                int b = color.B;

                // Convert to gray with constant factors 0.2126, 0.7152, 0.0722
                var gray = (r * 6966 + g * 23436 + b * 2366) >> 15;
                r = g = b = gray;

                bmp.SetPixel(i, new ColorHolder(a, r, g, b));
            }
        }
    }
}
