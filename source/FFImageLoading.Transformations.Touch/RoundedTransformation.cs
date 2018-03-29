using System;
using UIKit;
using CoreGraphics;
using Foundation;

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

        protected override UIImage Transform(UIImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToRounded(sourceBitmap, (nfloat)Radius, CropWidthRatio, CropHeightRatio, BorderSize, BorderHexColor);
        }

        public static UIImage ToRounded(UIImage source, nfloat rad, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
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

            float cropX = (float)((sourceWidth - desiredWidth) / 2);
            float cropY = (float)((sourceHeight - desiredHeight) / 2);

            if (rad == 0)
                rad = (nfloat)(Math.Min(desiredWidth, desiredHeight) / 2);
            else
                rad = (nfloat)(rad * (desiredWidth + desiredHeight) / 2 / 500);

            UIGraphics.BeginImageContextWithOptions(new CGSize(desiredWidth, desiredHeight), false, (nfloat)0.0);

            try
            {
                using (var context = UIGraphics.GetCurrentContext())
                {
                    var clippedRect = new CGRect(0d, 0d, desiredWidth, desiredHeight);

                    context.BeginPath();

                    using (var path = UIBezierPath.FromRoundedRect(clippedRect, rad))
                    {
                        context.AddPath(path.CGPath);
                        context.Clip();
                    }

                    var drawRect = new CGRect(-cropX, -cropY, sourceWidth, sourceHeight);
                    source.Draw(drawRect);

                    if (borderSize > 0d)
                    {
                        borderSize = (borderSize * (desiredWidth + desiredHeight) / 2d / 1000d);
                        var borderRect = new CGRect((0d + borderSize/2d), (0d + borderSize/2d),
                            (desiredWidth - borderSize), (desiredHeight - borderSize));

                        context.BeginPath();

                        using (var path = UIBezierPath.FromRoundedRect(borderRect, rad))
                        {
                            context.SetStrokeColor(borderHexColor.ToUIColor().CGColor);
                            context.SetLineWidth((nfloat)borderSize);
                            context.AddPath(path.CGPath);
                            context.StrokePath();
                        }
                    }

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

