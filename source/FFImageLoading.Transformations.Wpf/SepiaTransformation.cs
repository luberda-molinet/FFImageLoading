using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class SepiaTransformation : TransformationBase
    {
        public SepiaTransformation()
        {
        }

        public override string Key
        {
            get { return "SepiaTransformation"; }
        }

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            ToSepia(bitmapSource);

            return bitmapSource;
        }

        public static void ToSepia(BitmapHolder bmp)
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

                var rNew = (int)Math.Min((.393 * r) + (.769 * g) + (.189 * (b)), 255.0);
                var gNew = (int)Math.Min((.349 * r) + (.686 * g) + (.168 * (b)), 255.0);
                var bNew = (int)Math.Min((.272 * r) + (.534 * g) + (.131 * (b)), 255.0);

                if (rNew > 255)
                    rNew = 255;

                if (gNew > 255)
                    gNew = 255;

                if (bNew > 255)
                    bNew = 255;

                bmp.SetPixel(i, new ColorHolder(a, rNew, gNew, bNew));
            }
        }
    }
}
