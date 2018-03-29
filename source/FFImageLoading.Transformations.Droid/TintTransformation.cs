using System;
using Android.Graphics;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
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
                var color = value.ToColor();
                R = color.R;
                G = color.G;
                B = color.B;
                A = color.A;
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

        protected override Bitmap Transform(Bitmap sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            if (EnableSolidColor)
                return ToSolidColor(sourceBitmap, R, G, B, A);

            RGBAWMatrix = FFColorMatrix.ColorToTintMatrix(R, G, B, A);

            return base.Transform(sourceBitmap, path, source, isPlaceholder, key);
        }

        public static Bitmap ToSolidColor(Bitmap sourceBitmap, int r, int g, int b, int a)
        {
            var config = sourceBitmap?.GetConfig();
            if (config == null)
            {
                config = Bitmap.Config.Argb8888;
            }

            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            Bitmap bitmap = Bitmap.CreateBitmap(width, height, config);

            using (Canvas canvas = new Canvas(bitmap))
            {
                using (Paint paint = new Paint())
                {
                    PorterDuffColorFilter cf = new PorterDuffColorFilter(Color.Argb(a, r, g, b), PorterDuff.Mode.SrcAtop);
                    paint.SetColorFilter(cf);
                    canvas.DrawBitmap(sourceBitmap, 0, 0, paint);
                    return bitmap;
                }
            }
        }
    }
}

