using System;
using CoreGraphics;
using Foundation;
using AppKit;

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
                color.GetRgba(out r, out g, out b, out a);
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

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            if (EnableSolidColor)
                return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() => ToSolidColor(sourceBitmap, R, G, B, A));
            
            RGBAWMatrix = FFColorMatrix.ColorToTintMatrix(R, G, B, A);

            return base.Transform(sourceBitmap, path, source, isPlaceholder, key);
        }

        public static NSImage ToSolidColor(NSImage imageSource, int r, int g, int b, int a)
        {
            CGRect drawRect = new CGRect(0d, 0d, imageSource.CGImage.Width, imageSource.CGImage.Height);

            var colorSpace = CGColorSpace.CreateDeviceRGB();
            const int bytesPerPixel = 4;
            int width = (int)imageSource.CGImage.Width;
            int height = (int)imageSource.CGImage.Height;
            var bytes = new byte[width * height * bytesPerPixel];
            int bytesPerRow = bytesPerPixel * width;
            const int bitsPerComponent = 8;

            using (var context = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big))
            {
                context.TranslateCTM(0, drawRect.Size.Height);
                context.ScaleCTM(1.0f, -1.0f);
                context.ClipToMask(drawRect, imageSource.CGImage);
                context.SetFillColor(new CGColor(r, g, b, a));
                context.FillRect(drawRect);

                using (var output = context.ToImage())
                {
                    return new NSImage(output, CGSize.Empty);
                }
            }
        }
    }
}

