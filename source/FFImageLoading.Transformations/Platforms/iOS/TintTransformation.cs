using System;
using CoreGraphics;
using Foundation;
using UIKit;

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
                var color = value.ToUIColor();
                nfloat r, g, b, a;
                color.GetRGBA(out r, out g, out b, out a);
                R = (int)(255 * r);
                G = (int)(255 * g);
                B = (int)(255 * b);
                A = (int)(255 * a);
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

        protected override UIImage Transform(UIImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            if (EnableSolidColor)
                return ToSolidColor(sourceBitmap, R, G, B, A);

            RGBAWMatrix = FFColorMatrix.ColorToTintMatrix(R, G, B, A);

            return base.Transform(sourceBitmap, path, source, isPlaceholder, key);
        }

        public static UIImage ToSolidColor(UIImage imageSource, int r, int g, int b, int a)
        {
            CGRect drawRect = new CGRect(0.0, 0.0, imageSource.Size.Width, imageSource.Size.Height);

            UIGraphics.BeginImageContextWithOptions(imageSource.Size, false, 0.0f);
            using (var context = UIGraphics.GetCurrentContext())
            {
                context.TranslateCTM(0, drawRect.Size.Height);
                context.ScaleCTM(1.0f, -1.0f);
                context.ClipToMask(drawRect, imageSource.CGImage);
                context.SetFillColor(UIColor.FromRGBA(r, g, b, a).CGColor);
                context.FillRect(drawRect);
                var tintedImage = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();
                return tintedImage;
            }
        }
    }
}

