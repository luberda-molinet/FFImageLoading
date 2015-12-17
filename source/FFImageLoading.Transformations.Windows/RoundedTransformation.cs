using FFImageLoading.Work;
using System;
using Windows.UI;

namespace FFImageLoading.Transformations
{
    public class RoundedTransformation : TransformationBase
    {
        private double _radius;
        private double _cropWidthRatio;
        private double _cropHeightRatio;

        public RoundedTransformation(double radius) : this(radius, 1d, 1d)
        {
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio)
        {
            _radius = radius;
            _cropWidthRatio = cropWidthRatio;
            _cropHeightRatio = cropHeightRatio;
        }

        public override string Key
        {
            get
            {
                return string.Format("RoundedTransformation,radius={0},cropWidthRatio={1},cropHeightRatio={2}",
              _radius, _cropWidthRatio, _cropHeightRatio);
            }
        }

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            ToRounded(source, (int)_radius, _cropWidthRatio, _cropHeightRatio);

            return source;
        }

        public static void ToRounded(BitmapHolder source, int rad, double cropWidthRatio, double cropHeightRatio)
        {
            double sourceWidth = source.Width;
            double sourceHeight = source.Height;

            double desiredWidth = sourceWidth;
            double desiredHeight = sourceHeight;

            double desiredRatio = cropWidthRatio / cropHeightRatio;
            double currentRatio = sourceWidth / sourceHeight;

            if (currentRatio > desiredRatio)
                desiredWidth = (cropWidthRatio * sourceHeight / cropHeightRatio);
            else if (currentRatio < desiredRatio)
                desiredHeight = (cropHeightRatio * sourceWidth / cropWidthRatio);

            double cropX = ((sourceWidth - desiredWidth) / 2);
            double cropY = ((sourceHeight - desiredHeight) / 2);

            if (cropX != 0 || cropY != 0)
            {
                CropTransformation.ToCropped(source, (int)cropX, (int)cropY, (int)(desiredWidth), (int)(desiredHeight));
            }

            if (rad == 0)
                rad = (int)(Math.Min(desiredWidth, desiredHeight) / 2);
            else rad = (int)(rad * (desiredWidth + desiredHeight) / 2 / 500);

            int w = (int)desiredWidth;
            int h = (int)desiredHeight;

            int transparentColor = Colors.Transparent.ToInt();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x <= rad && y <= rad)
                    { //top left corner
                        if (!CheckRoundedCorner(rad, rad, rad, Corner.TopLeftCorner, x, y))
                            source.Pixels[y * w + x] = transparentColor;
                    }
                    else if (x >= w - rad && y <= rad)
                    { // top right corner
                        if (!CheckRoundedCorner(w - rad, rad, rad, Corner.TopRightCorner, x, y))
                            source.Pixels[y * w + x] = transparentColor;
                    }
                    else if (x >= w - rad && y >= h - rad)
                    { // bottom right corner
                        if (!CheckRoundedCorner(w - rad, h - rad, rad, Corner.BottomRightCorner, x, y))
                            source.Pixels[y * w + x] = transparentColor;
                    }
                    else if (x <= rad && y >= h - rad)
                    { // bottom left corner
                        if (!CheckRoundedCorner(rad, h - rad, rad, Corner.BottomLeftCorner, x, y))
                            source.Pixels[y * w + x] = transparentColor;
                    }
                }
            }
        }

        private enum Corner
        {
            TopLeftCorner,
            TopRightCorner,
            BottomRightCorner,
            BottomLeftCorner,
        }

        private static bool CheckRoundedCorner(int h, int k, int r, Corner which, int xC, int yC)
        {
            int x = 0;
            int y = r;
            int p = (3 - (2 * r));

            do
            {
                switch (which)
                {
                    case Corner.TopLeftCorner:
                        {   //Testing if its outside the top left corner
                            if (xC <= h - x && yC <= k - y) return false;
                            else if (xC <= h - y && yC <= k - x) return false;
                            break;
                        }
                    case Corner.TopRightCorner:
                        {   //Testing if its outside the top right corner
                            if (xC >= h + y && yC <= k - x) return false;
                            else if (xC >= h + x && yC <= k - y) return false;
                            break;
                        }
                    case Corner.BottomRightCorner:
                        {   //Testing if its outside the bottom right corner
                            if (xC >= h + x && yC >= k + y) return false;
                            else if (xC >= h + y && yC >= k + x) return false;
                            break;
                        }
                    case Corner.BottomLeftCorner:
                        {   //Testing if its outside the bottom left corner
                            if (xC <= h - y && yC >= k + x) return false;
                            else if (xC <= h - x && yC >= k + y) return false;
                            break;
                        }
                }

                x++;

                if (p < 0)
                {
                    p += ((4 * x) + 6);
                }  
                else
                {
                    y--;
                    p += ((4 * (x - y)) + 10);
                }
            } while (x <= y);

            return true;
        }
    }
}
