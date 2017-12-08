using System;
using AppKit;
using CoreGraphics;
using Foundation;
using FFImageLoading.Helpers;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RoundedTransformation : TransformationBase
    {
        public RoundedTransformation() : this(30d)
        {
        }

        public RoundedTransformation(double radius) : this(radius, 1d, 1d)
        {
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio) : this(radius, cropWidthRatio, cropHeightRatio, 0d, null)
        {
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
        {
            Radius = radius;
            CropWidthRatio = cropWidthRatio;
            CropHeightRatio = cropHeightRatio;
            BorderSize = borderSize;
            BorderHexColor = borderHexColor;
        }

        public override string Key
        {
            get
            {
                return string.Format("RoundedTransformation,radius={0},cropWidthRatio={1},cropHeightRatio={2},borderSize={3},borderHexColor={4}",
                Radius, CropWidthRatio, CropHeightRatio, BorderSize, BorderHexColor);
            }
        }

        public double Radius { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }
        public double BorderSize { get; set; }
        public string BorderHexColor { get; set; }

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() => ToRounded(sourceBitmap, (nfloat)Radius, CropWidthRatio, CropHeightRatio, BorderSize, BorderHexColor));
        }

        public static NSImage ToRounded(NSImage source, nfloat rad, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
        {
            double sourceWidth = source.CGImage.Width;
            double sourceHeight = source.CGImage.Height;

            double desiredWidth = sourceWidth;
            double desiredHeight = sourceHeight;

            double desiredRatio = cropWidthRatio / cropHeightRatio;
            double currentRatio = sourceWidth / sourceHeight;

            if (currentRatio > desiredRatio)
                desiredWidth = (cropWidthRatio * sourceHeight / cropHeightRatio);
            else if (currentRatio < desiredRatio)
                desiredHeight = (cropHeightRatio * sourceWidth / cropWidthRatio);

            float cropX = (float)((sourceWidth - desiredWidth) / 2);
            float cropY = (float)((sourceHeight - desiredHeight) / 2);

            if (rad == 0)
                rad = (nfloat)(Math.Min(desiredWidth, desiredHeight) / 2);
            else
                rad = (nfloat)(rad * (desiredWidth + desiredHeight) / 2 / 500);
            
            var colorSpace = CGColorSpace.CreateDeviceRGB();
            const int bytesPerPixel = 4;
            int width = (int)desiredWidth;
            int height = (int)desiredHeight;
            var bytes = new byte[width * height * bytesPerPixel];
            int bytesPerRow = bytesPerPixel * width;
            const int bitsPerComponent = 8;

            using (var context = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big))
            {
                var clippedRect = new CGRect(0d, 0d, desiredWidth, desiredHeight);

                context.BeginPath();

                using (var path = NSBezierPath.FromRoundedRect(clippedRect, rad, rad))
                {
                    context.AddPath(path.ToCGPath());
                    context.Clip();
                }

                var drawRect = new CGRect(-cropX, -cropY, sourceWidth, sourceHeight);
                context.DrawImage(drawRect, source.CGImage);

                if (borderSize > 0d)
                {
                    borderSize = (borderSize * (desiredWidth + desiredHeight) / 2d / 1000d);
                    var borderRect = new CGRect((0d + borderSize / 2d), (0d + borderSize / 2d),
                        (desiredWidth - borderSize), (desiredHeight - borderSize));

                    context.BeginPath();

                    using (var path = NSBezierPath.FromRoundedRect(borderRect, rad, rad))
                    {
                        context.SetStrokeColor(borderHexColor.ToUIColor().CGColor);
                        context.SetLineWidth((nfloat)borderSize);
                        context.AddPath(path.ToCGPath());
                        context.StrokePath();
                    }
                }

                using (var output = context.ToImage())
                {
                    return new NSImage(output, CGSize.Empty);
                }
            }
        }
    }
}

