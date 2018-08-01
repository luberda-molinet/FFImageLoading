using System;
using UIKit;
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

        protected override UIImage Transform(UIImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToTransformedCorners(sourceBitmap, TopLeftCornerSize, TopRightCornerSize, BottomLeftCornerSize, BottomRightCornerSize,
                CornersTransformType, CropWidthRatio, CropHeightRatio);
        }

        public static UIImage ToTransformedCorners(UIImage source, double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            double sourceWidth = source.Size.Width;
            double sourceHeight = source.Size.Height;

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

            UIGraphics.BeginImageContextWithOptions(new CGSize(desiredWidth, desiredHeight), false, (nfloat)0.0);

            try
            {
                using (var context = UIGraphics.GetCurrentContext())
                {
                    context.BeginPath();

                    using (var path = new UIBezierPath())
                    {
                        // TopLeft
                        if (cornersTransformType.HasFlag(CornerTransformType.TopLeftCut))
                        {
                            path.MoveTo(new CGPoint(0, topLeftCornerSize));
                            path.AddLineTo(new CGPoint(topLeftCornerSize, 0));
                        }
                        else if (cornersTransformType.HasFlag(CornerTransformType.TopLeftRounded))
                        {
                            path.MoveTo(new CGPoint(0, topLeftCornerSize));
                            path.AddQuadCurveToPoint(new CGPoint(topLeftCornerSize, 0), new CGPoint(0, 0));
                        }
                        else
                        {
                            path.MoveTo(new CGPoint(0, 0));
                        }

                        // TopRight
                        if (cornersTransformType.HasFlag(CornerTransformType.TopRightCut))
                        {
                            path.AddLineTo(new CGPoint(desiredWidth - topRightCornerSize, 0));
                            path.AddLineTo(new CGPoint(desiredWidth, topRightCornerSize));
                        }
                        else if (cornersTransformType.HasFlag(CornerTransformType.TopRightRounded))
                        {
                            path.AddLineTo(new CGPoint(desiredWidth - topRightCornerSize, 0));
                            path.AddQuadCurveToPoint(new CGPoint(desiredWidth, topRightCornerSize), new CGPoint(desiredWidth, 0));
                        }
                        else
                        {
                            path.AddLineTo(new CGPoint(desiredWidth ,0));
                        }

                        // BottomRight
                        if (cornersTransformType.HasFlag(CornerTransformType.BottomRightCut))
                        {
                            path.AddLineTo(new CGPoint(desiredWidth, desiredHeight - bottomRightCornerSize));
                            path.AddLineTo(new CGPoint(desiredWidth - bottomRightCornerSize, desiredHeight));
                        }
                        else if (cornersTransformType.HasFlag(CornerTransformType.BottomRightRounded))
                        {
                            path.AddLineTo(new CGPoint(desiredWidth, desiredHeight - bottomRightCornerSize));
                            path.AddQuadCurveToPoint(new CGPoint(desiredWidth - bottomRightCornerSize, desiredHeight), new CGPoint(desiredWidth, desiredHeight));
                        }
                        else
                        {
                            path.AddLineTo(new CGPoint(desiredWidth, desiredHeight));
                        }

                        // BottomLeft
                        if (cornersTransformType.HasFlag(CornerTransformType.BottomLeftCut))
                        {
                            path.AddLineTo(new CGPoint(bottomLeftCornerSize, desiredHeight));
                            path.AddLineTo(new CGPoint(0, desiredHeight - bottomLeftCornerSize));
                        }
                        else if (cornersTransformType.HasFlag(CornerTransformType.BottomLeftRounded))
                        {
                            path.AddLineTo(new CGPoint(bottomLeftCornerSize, desiredHeight));
                            path.AddQuadCurveToPoint(new CGPoint(0, desiredHeight - bottomLeftCornerSize), new CGPoint(0, desiredHeight));
                        }
                        else
                        {
                            path.AddLineTo(new CGPoint(0, desiredHeight));
                        }

                        path.ClosePath();
                        context.AddPath(path.CGPath);
                        context.Clip();
                    }

                    var drawRect = new CGRect(-cropX, -cropY, sourceWidth, sourceHeight);
                    source.Draw(drawRect);
                    var modifiedImage = UIGraphics.GetImageFromCurrentImageContext();

                    return modifiedImage;
                }
            }
            finally
            {
                UIGraphics.EndImageContext();
            }
        }
    }
}

