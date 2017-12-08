using System;
using AppKit;
using CoreGraphics;
using Foundation;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CornersTransformation : TransformationBase
    {
        public CornersTransformation() : this(20d, CornerTransformType.TopRightRounded)
        {
        }

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType)
            : this(cornersSize, cornersSize, cornersSize, cornersSize, cornersTransformType, 1d, 1d)
        {
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType)
            : this(topLeftCornerSize, topRightCornerSize, bottomLeftCornerSize, bottomRightCornerSize, cornersTransformType, 1d, 1d)
        {
        }

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
            : this(cornersSize, cornersSize, cornersSize, cornersSize, cornersTransformType, cropWidthRatio, cropHeightRatio)
        {
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            TopLeftCornerSize = topLeftCornerSize;
            TopRightCornerSize = topRightCornerSize;
            BottomLeftCornerSize = bottomLeftCornerSize;
            BottomRightCornerSize = bottomRightCornerSize;
            CornersTransformType = cornersTransformType;
            CropWidthRatio = cropWidthRatio;
            CropHeightRatio = cropHeightRatio;
        }

        public double TopLeftCornerSize { get; set; }
        public double TopRightCornerSize { get; set; }
        public double BottomLeftCornerSize { get; set; }
        public double BottomRightCornerSize { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }
        public CornerTransformType CornersTransformType { get; set; }

        public override string Key
        {
            get
            {
                return string.Format("CornersTransformation,cornersSizes={0},{1},{2},{3},cornersTransformType={4},cropWidthRatio={5},cropHeightRatio={6},",
              TopLeftCornerSize, TopRightCornerSize, BottomRightCornerSize, BottomLeftCornerSize, CornersTransformType, CropWidthRatio, CropHeightRatio);
            }
        }

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() => ToTransformedCorners(sourceBitmap, TopLeftCornerSize, TopRightCornerSize, BottomLeftCornerSize, BottomRightCornerSize,
                                                                                                  CornersTransformType, CropWidthRatio, CropHeightRatio));
        }

        public static NSImage ToTransformedCorners(NSImage source, double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
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

            topLeftCornerSize = topLeftCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            topRightCornerSize = topRightCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            bottomLeftCornerSize = bottomLeftCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            bottomRightCornerSize = bottomRightCornerSize * (desiredWidth + desiredHeight) / 2 / 100;

            float cropX = (float)((sourceWidth - desiredWidth) / 2);
            float cropY = (float)((sourceHeight - desiredHeight) / 2);

            var colorSpace = CGColorSpace.CreateDeviceRGB();
            const int bytesPerPixel = 4;
            int width = (int)desiredWidth;
            int height = (int)desiredHeight;
            var bytes = new byte[width * height * bytesPerPixel];
            int bytesPerRow = bytesPerPixel * width;
            const int bitsPerComponent = 8;

            using (var context = new CGBitmapContext(bytes, width, height, bitsPerComponent, bytesPerRow, colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big))
            {
                context.BeginPath();

                using (var path = new NSBezierPath())
                {
                    // TopLeft
                    if (cornersTransformType.HasFlag(CornerTransformType.TopLeftCut))
                    {
                        path.MoveTo(new CGPoint(0, topLeftCornerSize));
                        path.LineTo(new CGPoint(topLeftCornerSize, 0));
                    }
                    else if (cornersTransformType.HasFlag(CornerTransformType.TopLeftRounded))
                    {
                        path.MoveTo(new CGPoint(0, topLeftCornerSize));
                        path.QuadCurveToPoint(new CGPoint(topLeftCornerSize, 0), new CGPoint(0, 0));
                    }
                    else
                    {
                        path.MoveTo(new CGPoint(0, 0));
                    }

                    // TopRight
                    if (cornersTransformType.HasFlag(CornerTransformType.TopRightCut))
                    {
                        path.LineTo(new CGPoint(desiredWidth - topRightCornerSize, 0));
                        path.LineTo(new CGPoint(desiredWidth, topRightCornerSize));
                    }
                    else if (cornersTransformType.HasFlag(CornerTransformType.TopRightRounded))
                    {
                        path.LineTo(new CGPoint(desiredWidth - topRightCornerSize, 0));
                        path.QuadCurveToPoint(new CGPoint(desiredWidth, topRightCornerSize), new CGPoint(desiredWidth, 0));
                    }
                    else
                    {
                        path.LineTo(new CGPoint(desiredWidth, 0));
                    }

                    // BottomRight
                    if (cornersTransformType.HasFlag(CornerTransformType.BottomRightCut))
                    {
                        path.LineTo(new CGPoint(desiredWidth, desiredHeight - bottomRightCornerSize));
                        path.LineTo(new CGPoint(desiredWidth - bottomRightCornerSize, desiredHeight));
                    }
                    else if (cornersTransformType.HasFlag(CornerTransformType.BottomRightRounded))
                    {
                        path.LineTo(new CGPoint(desiredWidth, desiredHeight - bottomRightCornerSize));
                        path.QuadCurveToPoint(new CGPoint(desiredWidth - bottomRightCornerSize, desiredHeight), new CGPoint(desiredWidth, desiredHeight));
                    }
                    else
                    {
                        path.LineTo(new CGPoint(desiredWidth, desiredHeight));
                    }

                    // BottomLeft
                    if (cornersTransformType.HasFlag(CornerTransformType.BottomLeftCut))
                    {
                        path.LineTo(new CGPoint(bottomLeftCornerSize, desiredHeight));
                        path.LineTo(new CGPoint(0, desiredHeight - bottomLeftCornerSize));
                    }
                    else if (cornersTransformType.HasFlag(CornerTransformType.BottomLeftRounded))
                    {
                        path.LineTo(new CGPoint(bottomLeftCornerSize, desiredHeight));
                        path.QuadCurveToPoint(new CGPoint(0, desiredHeight - bottomLeftCornerSize), new CGPoint(0, desiredHeight));
                    }
                    else
                    {
                        path.LineTo(new CGPoint(0, desiredHeight));
                    }

                    path.ClosePath();
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

