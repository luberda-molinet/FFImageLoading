using System;
using CoreGraphics;
using FFImageLoading.Work;
using UIKit;

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

        protected override UIImage Transform(UIImage sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            double sourceWidth = sourceBitmap.Size.Width;
            double sourceHeight = sourceBitmap.Size.Height;

            UIGraphics.BeginImageContextWithOptions(new CGSize(sourceWidth, sourceHeight), false, (nfloat)0.0);

            try
            {
                using (var context = UIGraphics.GetCurrentContext())
                {

					var drawRect = new CGRect(0.0, 0.0, sourceBitmap.Size.Width, sourceBitmap.Size.Height);

					context.TranslateCTM(0, sourceBitmap.Size.Height);
					context.ScaleCTM(1.0f, -1.0f);
                    context.SetFillColor(HexColor.ToUIColor().CGColor);
                    context.FillRect(drawRect);
                    context.DrawImage(drawRect, sourceBitmap.CGImage);

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
