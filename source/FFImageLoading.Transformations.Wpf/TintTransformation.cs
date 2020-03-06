using FFImageLoading.Extensions;
using FFImageLoading.Work;
using System;

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
                A = color.A;
                R = color.R;
                G = color.G;
                B = color.B;
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
            float percentage = (float)a / 255;
            float left = 1 - percentage;
            int rMin = (int)(r - (r * left));
            int gMin = (int)(g - (g * left));
            int bMin = (int)(b - (b * left));
            int rMax = (int)(r + (r * left));
            int gMax = (int)(g + (g * left));
            int bMax = (int)(b + (b * left));

            for (var i = 0; i < len; i++)
            {
                var color = bmp.GetPixel(i);
                int currentAlpha = color.A;
                var curR = color.R;
                var curG = color.G;
                var curB = color.B;
                int rNew = (int)(curR + (255 - curR) * (percentage * r / 255));
                int gNew = (int)(curG + (255 - curG) * (percentage * g / 255));
                int bNew = (int)(curB + (255 - curB) * (percentage * b / 255));
                rNew = Math.Min(Math.Max(rMin, rNew), rMax);
                gNew = Math.Min(Math.Max(gMin, gNew), gMax);
                bNew = Math.Min(Math.Max(bMin, bNew), bMax);

                bmp.SetPixel(i, new ColorHolder(color.A, rNew, gNew, bNew));
            }
        }
    }
}
