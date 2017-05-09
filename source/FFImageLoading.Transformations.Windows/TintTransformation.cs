using FFImageLoading.Extensions;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class TintTransformation : ColorSpaceTransformation
    {
        public TintTransformation() : this(0, 165, 0, 128)
        {
        }

        public TintTransformation(int r, int g, int b, int a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public TintTransformation(string hexColor)
        {
            HexColor = hexColor;
        }

        string _hexColor;
        public string HexColor
        {
            get
            {
                return _hexColor;
            }

            set
            {
                _hexColor = value;
                var color = value.ToColorFromHex();
                R = (int)color.R;
                G = (int)color.G;
                B = (int)color.B;
                A = (int)color.A;
            }
        }

        public bool EnableSolidColor { get; set; }

        public int R { get; set; }

        public int G { get; set; }

        public int B { get; set; }

        public int A { get; set; }

        public override string Key
        {
            get
            {
                return string.Format("TintTransformation,R={0},G={1},B={2},A={3},HexColor={4},EnableSolidColor={5}",
                                     R, G, B, A, HexColor, EnableSolidColor);
            }
        }

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            if (EnableSolidColor)
            {
                ToReplacedColor(bitmapSource, R, G, B, A);
                return bitmapSource;
            }

            RGBAWMatrix = FFColorMatrix.ColorToTintMatrix(R, G, B, A);

            return base.Transform(bitmapSource, path, source, isPlaceholder, key);
        }

        public static void ToReplacedColor(BitmapHolder bmp, int r, int g, int b, int a)
        {
            var nWidth = bmp.Width;
            var nHeight = bmp.Height;
            var len = bmp.PixelCount;

            for (var i = 0; i < len; i++)
            {
                var c = bmp.GetPixelAsInt(i);
                var currentAlpha = (c >> 24) & 0x000000FF;
                var aNew = (int)(currentAlpha * (a / currentAlpha));

                var rNew = r;
                var gNew = g;
                var bNew = b;

                if (rNew > 255)
                    rNew = 255;

                if (gNew > 255)
                    gNew = 255;

                if (bNew > 255)
                    bNew = 255;

                bmp.SetPixel(i, (aNew << 24) | (rNew << 16) | (gNew << 8) | bNew);
            }
        }
    }
}
