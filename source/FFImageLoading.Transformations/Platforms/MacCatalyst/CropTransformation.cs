using System;
using UIKit;
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

        protected override UIImage Transform(UIImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToCropped(sourceBitmap, ZoomFactor, XOffset, YOffset, CropWidthRatio, CropHeightRatio);
        }

        public static UIImage ToCropped(UIImage source, double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
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

            UIGraphics.BeginImageContextWithOptions(new CGSize(desiredWidth, desiredHeight), false, (nfloat)0.0);

            try
            {
                using (var context = UIGraphics.GetCurrentContext())
                {
                    var clippedRect = new CGRect(0, 0, desiredWidth, desiredHeight);
                    context.BeginPath();

                    using (var path = UIBezierPath.FromRect(clippedRect))
                    {
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

