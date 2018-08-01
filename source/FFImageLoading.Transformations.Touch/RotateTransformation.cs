using System;
using UIKit;
using CoreGraphics;
using Foundation;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RotateTransformation : TransformationBase
    {
        public RotateTransformation() : this(30d)
        {
        }

        public RotateTransformation(double degrees) : this(degrees, false, false)
        {
        }

        public RotateTransformation(double degrees, bool ccw) : this(degrees, ccw, false)
        {
        }

        public RotateTransformation(double degrees, bool ccw, bool resize)
        {
            Degrees = degrees;
            CCW = ccw;
            Resize = resize;
        }

        public double Degrees { get; set; }
        public bool CCW { get; set; }
        public bool Resize { get; set; }

        public override string Key
        {
            get { return string.Format("RotateTransformation,degrees={0},ccw={1},resize={2}", Degrees, CCW, Resize); }
        }

        protected override UIImage Transform(UIImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToRotated(sourceBitmap, Degrees, CCW, Resize);
        }

        public static UIImage ToRotated(UIImage source, double degrees, bool ccw, bool resize)
        {
            if (degrees == 0 || degrees % 360 == 0)
                return source;

            if (ccw)
                degrees = 360d - degrees;

            CGRect boundingRect = new CGRect(0, 0, source.Size.Width, source.Size.Height);

            if (resize && (degrees % 180 != 0))
                boundingRect = GetBoundingRectAfterRotation(new CGRect(0, 0, source.Size.Width, source.Size.Height), degrees);

            UIGraphics.BeginImageContextWithOptions(new CGSize(boundingRect.Size.Width, boundingRect.Size.Height), false, (nfloat)0.0);

            try
            {
                using (var context = UIGraphics.GetCurrentContext())
                {
                    context.TranslateCTM((nfloat)(boundingRect.Size.Width / 2.0), (nfloat)(boundingRect.Size.Height / 2.0));
                    context.RotateCTM((nfloat)DegreesToRadians(degrees));
                    context.ScaleCTM((nfloat)1.0, (nfloat)(-1.0));

                    var newRect = new CGRect(-source.Size.Width / 2, -source.Size.Height / 2, source.Size.Width, source.Size.Height);
                    context.DrawImage(newRect, source.CGImage);

                    var modifiedImage = UIGraphics.GetImageFromCurrentImageContext();

                    return modifiedImage;
                }
            }
            finally
            {
                UIGraphics.EndImageContext();
            }
        }

        private static double RadiansToDegrees(double angle)
        {
            return angle * (180.0d / Math.PI);
        }

        private static double DegreesToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        private static CGRect GetBoundingRectAfterRotation(CGRect rectangle, double degrees)
        {
            var angleOfRotation = DegreesToRadians(degrees);

            // Calculate the width and height of the bounding rectangle using basic trig
            double newWidth = rectangle.Size.Width * Math.Abs(Math.Cos(angleOfRotation)) + rectangle.Size.Height * Math.Abs(Math.Sin(angleOfRotation));
            double newHeight = rectangle.Size.Height * Math.Abs(Math.Cos(angleOfRotation)) + rectangle.Size.Width * Math.Abs(Math.Sin(angleOfRotation));

            // Calculate the position of the origin
            double newX = rectangle.Location.X + ((rectangle.Size.Width - newWidth) / 2);
            double newY = rectangle.Location.Y + ((rectangle.Size.Height - newHeight) / 2);

            // Return the rectangle
            return new CGRect(newX, newY, newWidth, newHeight);
        }
    }
}

