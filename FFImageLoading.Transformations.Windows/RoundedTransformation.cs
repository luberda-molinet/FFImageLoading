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

        public RoundedTransformation(double radius)
        {
            _radius = radius;
            _cropWidthRatio = 1f;
            _cropHeightRatio = 1f;
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
                return string.Format("RoundedTransformation, radius = {0}, cropWidthRatio = {1}, cropHeightRatio = {2}",
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
            else rad = (int)(rad * (desiredWidth + desiredHeight) / 2 / 600);

            int x = 0;
            int y = 0;
            int w = (int)desiredWidth;
            int h = (int)desiredHeight;

            int atx = 0;
            int aty = 0;
            int j = 0;
            int k = 0;
            int index = 0;

            bool shouldDraw = false;

            int transparentColor = Colors.Transparent.ToInt();

            for (k = 0; k < h; k++)
            {
                for (j = 0; j < w; j++)
                {
                    shouldDraw = false;

                    atx = x + j;
                    aty = y + k;

                    if (atx <= x + rad && aty <= y + rad)
                    { //top left corner
                        if (CheckCorner(x + rad, y + rad, rad, 1, atx, aty))
                        {
                            shouldDraw = true;
                        }
                    }
                    else if (atx >= x + w - rad && aty <= y + rad)
                    { // top right corner
                        if (CheckCorner(x + w - rad, y + rad, rad, 2, atx, aty))
                        {
                            shouldDraw = true;
                        }
                    }
                    else if (atx >= x + w - rad && aty >= y + h - rad)
                    { // bottom right corner
                        if (CheckCorner(x + w - rad, y + h - rad, rad, 3, atx, aty))
                        {
                            shouldDraw = true;
                        }
                    }
                    else if (atx <= x + rad && aty >= y + h - rad)
                    { // bottom left corner
                        if (CheckCorner(x + rad, y + h - rad, rad, 4, atx, aty))
                        {
                            shouldDraw = true;
                        }
                    }
                    else
                    { // its not in a corner
                        shouldDraw = true;
                    }

                    if (!shouldDraw)
                    {
                        source.Pixels[aty * w + atx] = transparentColor;
                    }

                    index++;
                }
            }

            x++;
        }

        private static bool CheckCorner(int h, int k, int r, int which, int xC, int yC)
        {
            int x = 0;
            int y = r;
            int p = (3 - (2 * r));

            do
            {
                switch (which)
                {
                    case 1:
                        {   //Testing if its outside the top left corner
                            if (xC <= h - x && yC <= k - y)
                            {
                                return false;
                            }
                            else if (xC <= h - y && yC <= k - x)
                            {
                                return false;
                            }
                            break;
                        }
                    case 2:
                        {   //Testing if its outside the top right corner
                            if (xC >= h + y && yC <= k - x)
                            {
                                return false;
                            }
                            else if (xC >= h + x && yC <= k - y)
                            {
                                return false;
                            }
                            break;
                        }
                    case 3:
                        {   //Testing if its outside the bottom right corner
                            if (xC >= h + x && yC >= k + y)
                            {
                                return false;
                            }
                            else if (xC >= h + y && yC >= k + x)
                            {
                                return false;
                            }
                            break;
                        }
                    case 4:
                        {   //Testing if its outside the bottom left corner
                            if (xC <= h - y && yC >= k + x)
                            {
                                return false;
                            }
                            else if (xC <= h - x && yC >= k + y)
                            {
                                return false;
                            }
                            break;
                        }
                }

                x++;

                if (p < 0)
                    p += ((4 * x) + 6);

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
