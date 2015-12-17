using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class RotateTransformation : TransformationBase
    {
        double _degrees;
        bool _ccw;
        bool _resize;

        public RotateTransformation(double degrees) : this(degrees, false, false)
        {
        }

        public RotateTransformation(double degrees, bool ccw) : this(degrees, ccw, false)
        {
        }

        public RotateTransformation(double degrees, bool ccw, bool resize)
        {
            _degrees = degrees;
            _ccw = ccw;
            _resize = resize;
        }

        public override string Key
        {
            get { return string.Format("RotateTransformation, degrees = {0}, ccw = {1}, resize = {2}", _degrees, _ccw, _resize); }
        }

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            ToRotated(source, _degrees, _ccw, _resize);

            return source;
        }

        public static BitmapHolder ToRotated(BitmapHolder source, double degrees, bool ccw, bool resize)
        {
            if (ccw)
                degrees = 360d - degrees;

            // rotating clockwise, so it's negative relative to Cartesian quadrants
            double cnAngle = -1.0 * (Math.PI / 180) * degrees;

            // general iterators
            int i, j;
            // calculated indices in Cartesian coordinates
            int x, y;
            double fDistance, fPolarAngle;
            // for use in neighboring indices in Cartesian coordinates
            int iFloorX, iCeilingX, iFloorY, iCeilingY;
            // calculated indices in Cartesian coordinates with trailing decimals
            double fTrueX, fTrueY;
            // for interpolation
            double fDeltaX, fDeltaY;

            // interpolated "top" pixels
            double fTopRed, fTopGreen, fTopBlue, fTopAlpha;

            // interpolated "bottom" pixels
            double fBottomRed, fBottomGreen, fBottomBlue, fBottomAlpha;

            // final interpolated color components
            int iRed, iGreen, iBlue, iAlpha;

            int iCentreX, iCentreY;
            int iDestCentreX, iDestCentreY;
            int iWidth, iHeight, newWidth, newHeight;

            iWidth = source.Width;
            iHeight = source.Height;

            if (resize)
            {
                newWidth = iWidth;
                newHeight = iHeight;
            }
            else
            {
                var rad = degrees / (180 / Math.PI);
                newWidth = (int)Math.Ceiling(Math.Abs(Math.Sin(rad) * iHeight) + Math.Abs(Math.Cos(rad) * iWidth));
                newHeight = (int)Math.Ceiling(Math.Abs(Math.Sin(rad) * iWidth) + Math.Abs(Math.Cos(rad) * iHeight));
            }


            iCentreX = iWidth / 2;
            iCentreY = iHeight / 2;

            iDestCentreX = newWidth / 2;
            iDestCentreY = newHeight / 2;

            var newp = new int[newWidth * newHeight];
            var oldp = source.Pixels;
            var oldw = source.Width;

            // assigning pixels of destination image from source image
            // with bilinear interpolation
            for (i = 0; i < newHeight; ++i)
            {
                for (j = 0; j < newWidth; ++j)
                {
                    // convert raster to Cartesian
                    x = j - iDestCentreX;
                    y = iDestCentreY - i;

                    // convert Cartesian to polar
                    fDistance = Math.Sqrt(x * x + y * y);
                    if (x == 0)
                    {
                        if (y == 0)
                        {
                            // center of image, no rotation needed
                            newp[i * newWidth + j] = oldp[iCentreY * oldw + iCentreX];
                            continue;
                        }
                        if (y < 0)
                        {
                            fPolarAngle = 1.5 * Math.PI;
                        }
                        else
                        {
                            fPolarAngle = 0.5 * Math.PI;
                        }
                    }
                    else
                    {
                        fPolarAngle = Math.Atan2(y, x);
                    }

                    // the crucial rotation part
                    // "reverse" rotate, so minus instead of plus
                    fPolarAngle -= cnAngle;

                    // convert polar to Cartesian
                    fTrueX = fDistance * Math.Cos(fPolarAngle);
                    fTrueY = fDistance * Math.Sin(fPolarAngle);

                    // convert Cartesian to raster
                    fTrueX = fTrueX + iCentreX;
                    fTrueY = iCentreY - fTrueY;

                    iFloorX = (int)(Math.Floor(fTrueX));
                    iFloorY = (int)(Math.Floor(fTrueY));
                    iCeilingX = (int)(Math.Ceiling(fTrueX));
                    iCeilingY = (int)(Math.Ceiling(fTrueY));

                    // check bounds
                    if (iFloorX < 0 || iCeilingX < 0 || iFloorX >= iWidth || iCeilingX >= iWidth || iFloorY < 0 ||
                        iCeilingY < 0 || iFloorY >= iHeight || iCeilingY >= iHeight)
                        continue;

                    fDeltaX = fTrueX - iFloorX;
                    fDeltaY = fTrueY - iFloorY;

                    var clrTopLeft = oldp[iFloorY * oldw + iFloorX];
                    var clrTopRight = oldp[iFloorY * oldw + iCeilingX];
                    var clrBottomLeft = oldp[iCeilingY * oldw + iFloorX];
                    var clrBottomRight = oldp[iCeilingY * oldw + iCeilingX];

                    fTopAlpha = (1 - fDeltaX) * ((clrTopLeft >> 24) & 0xFF) + fDeltaX * ((clrTopRight >> 24) & 0xFF);
                    fTopRed = (1 - fDeltaX) * ((clrTopLeft >> 16) & 0xFF) + fDeltaX * ((clrTopRight >> 16) & 0xFF);
                    fTopGreen = (1 - fDeltaX) * ((clrTopLeft >> 8) & 0xFF) + fDeltaX * ((clrTopRight >> 8) & 0xFF);
                    fTopBlue = (1 - fDeltaX) * (clrTopLeft & 0xFF) + fDeltaX * (clrTopRight & 0xFF);

                    // linearly interpolate horizontally between bottom neighbors
                    fBottomAlpha = (1 - fDeltaX) * ((clrBottomLeft >> 24) & 0xFF) + fDeltaX * ((clrBottomRight >> 24) & 0xFF);
                    fBottomRed = (1 - fDeltaX) * ((clrBottomLeft >> 16) & 0xFF) + fDeltaX * ((clrBottomRight >> 16) & 0xFF);
                    fBottomGreen = (1 - fDeltaX) * ((clrBottomLeft >> 8) & 0xFF) + fDeltaX * ((clrBottomRight >> 8) & 0xFF);
                    fBottomBlue = (1 - fDeltaX) * (clrBottomLeft & 0xFF) + fDeltaX * (clrBottomRight & 0xFF);

                    // linearly interpolate vertically between top and bottom interpolated results
                    iRed = (int)(Math.Round((1 - fDeltaY) * fTopRed + fDeltaY * fBottomRed));
                    iGreen = (int)(Math.Round((1 - fDeltaY) * fTopGreen + fDeltaY * fBottomGreen));
                    iBlue = (int)(Math.Round((1 - fDeltaY) * fTopBlue + fDeltaY * fBottomBlue));
                    iAlpha = (int)(Math.Round((1 - fDeltaY) * fTopAlpha + fDeltaY * fBottomAlpha));

                    // make sure color values are valid
                    if (iRed < 0) iRed = 0;
                    if (iRed > 255) iRed = 255;
                    if (iGreen < 0) iGreen = 0;
                    if (iGreen > 255) iGreen = 255;
                    if (iBlue < 0) iBlue = 0;
                    if (iBlue > 255) iBlue = 255;
                    if (iAlpha < 0) iAlpha = 0;
                    if (iAlpha > 255) iAlpha = 255;

                    var a = iAlpha + 1;
                    newp[i * newWidth + j] = (iAlpha << 24)
                                           | ((byte)((iRed * a) >> 8) << 16)
                                           | ((byte)((iGreen * a) >> 8) << 8)
                                           | ((byte)((iBlue * a) >> 8));
                }
            }

            source.SetPixels(newp, newWidth, newHeight);

            return source;
        }
    }
}
