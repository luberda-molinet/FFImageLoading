using System;
using AppKit;
using CoreGraphics;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class ColorFillTransformation : TransformationBase
    {
        public ColorFillTransformation() : this("#000000")
        {
        }

        public ColorFillTransformation(string hexColor)
        {
            HexColor = hexColor;
        }

        public string HexColor { get; set; }

        public override string Key => string.Format("ColorFillTransformation,hexColor={0}", HexColor);

        protected override NSImage Transform(NSImage sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            var colorSpace = CGColorSpace.CreateDeviceRGB();
            const int bytesPerPixel = 4;
            int width = (int)sourceBitmap.CGImage.Width;
            int height = (int)sourceBitmap.CGImage.Height;
            var bytes = new byte[width * height * bytesPerPixel];
            int bytesPerRow = bytesPerPixel * width;
            const int bitsPerComponent = 8;

            using (var context = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big))
            {
                CGRect drawRect = new CGRect(0.0, 0.0, sourceBitmap.Size.Width, sourceBitmap.Size.Height);
                context.SetFillColor(HexColor.ToUIColor().CGColor);
                context.FillRect(drawRect);
                context.DrawImage(drawRect, sourceBitmap.CGImage);

                using (var output = context.ToImage())
                {
                    return new NSImage(output, CGSize.Empty);
                }
            }
        }
    }
}
