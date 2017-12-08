using System;
using AppKit;
using CoreGraphics;
using Foundation;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CropTransformation : TransformationBase
    {
        public CropTransformation() : this(1d, 0d, 0d)
        {
        }

        public CropTransformation(double zoomFactor, double xOffset, double yOffset) : this(zoomFactor, xOffset, yOffset, 1f, 1f)
        {
        }

        public CropTransformation(double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
        {
            ZoomFactor = zoomFactor;
            XOffset = xOffset;
            YOffset = yOffset;
            CropWidthRatio = cropWidthRatio;
            CropHeightRatio = cropHeightRatio;

            if (ZoomFactor < 1f)
                ZoomFactor = 1f;
        }

        public double ZoomFactor { get; set; }
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }

        public override string Key
        {
            get
            {
                return string.Format("CropTransformation,zoomFactor={0},xOffset={1},yOffset={2},cropWidthRatio={3},cropHeightRatio={4}",
                ZoomFactor, XOffset, YOffset, CropWidthRatio, CropHeightRatio);
            }
        }

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() => ToCropped(sourceBitmap, ZoomFactor, XOffset, YOffset, CropWidthRatio, CropHeightRatio));
        }

        public static NSImage ToCropped(NSImage source, double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
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

            xOffset = xOffset * desiredWidth;
            yOffset = yOffset * desiredHeight;

            desiredWidth =  desiredWidth / zoomFactor;
            desiredHeight = desiredHeight / zoomFactor;

            float cropX = (float)(((sourceWidth - desiredWidth) / 2) + xOffset);
            float cropY = (float)(((sourceHeight - desiredHeight) / 2) + yOffset);

            if (cropX < 0)
                cropX = 0;

            if (cropY < 0)
                cropY = 0;

            if (cropX + desiredWidth > sourceWidth)
                cropX = (float)(sourceWidth - desiredWidth);

            if (cropY + desiredHeight > sourceHeight)
                cropY = (float)(sourceHeight - desiredHeight);


            var colorSpace = CGColorSpace.CreateDeviceRGB();
            const int bytesPerPixel = 4;
            int width = (int)desiredWidth;
            int height = (int)desiredHeight;
            var bytes = new byte[width * height * bytesPerPixel];
            int bytesPerRow = bytesPerPixel * width;
            const int bitsPerComponent = 8;

            using (var context = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big))
            {
                var clippedRect = new CGRect(0, 0, desiredWidth, desiredHeight);
                context.BeginPath();

                using (var path = NSBezierPath.FromRect(clippedRect))
                {
                    context.AddPath(path.ToCGPath());
                    context.Clip();
                }

                var drawRect = new CGRect(-cropX, -cropY, sourceWidth, sourceHeight);
                context.DrawImage(drawRect, source.CGImage);

                using (var output = context.ToImage())
                {
                    return new NSImage(output, CGSize.Empty);
                }
            }
        }
    }
}

