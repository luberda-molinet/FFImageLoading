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

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            ToSepia(source);

            return source;
        }

        public static void ToSepia(BitmapHolder bmp)
        {
            var nWidth = bmp.Width;
            var nHeight = bmp.Height;
            var len = bmp.PixelCount;

            for (var i = 0; i < len; i++)
            {
                var c = bmp.GetPixelAsInt(i);
                var a = (c >> 24) & 0x000000FF;
                var r = (c >> 16) & 0x000000FF;
                var g = (c >> 8) & 0x000000FF;
                var b = (c) & 0x000000FF;

                var rNew = (int)Math.Min((.393 * r) + (.769 * g) + (.189 * (b)), 255.0);
                var gNew = (int)Math.Min((.349 * r) + (.686 * g) + (.168 * (b)), 255.0);
                var bNew = (int)Math.Min((.272 * r) + (.534 * g) + (.131 * (b)), 255.0);

                if (rNew > 255)
                    rNew = 255;

                if (gNew > 255)
                    gNew = 255;

                if (bNew > 255)
                    bNew = 255;

                bmp.SetPixel(i, (a << 24) | (rNew << 16) | (gNew << 8) | bNew);
            }
        }
    }
}
