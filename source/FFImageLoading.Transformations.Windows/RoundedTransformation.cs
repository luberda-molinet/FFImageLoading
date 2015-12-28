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

        private double _borderSize;
        private string _borderHexColor;

        public RoundedTransformation(double radius) : this(radius, 1d, 1d)
        {
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio) : this(radius, cropWidthRatio, cropHeightRatio, 0d, null)
        {
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
        {
            _radius = radius;
            _cropWidthRatio = cropWidthRatio;
            _cropHeightRatio = cropHeightRatio;
            _borderSize = borderSize;
            _borderHexColor = borderHexColor;
        }

        public override string Key
        {
            get
            {
                return string.Format("RoundedTransformation,radius={0},cropWidthRatio={1},cropHeightRatio={2},borderSize={3},borderHexColor={4}",
              _radius, _cropWidthRatio, _cropHeightRatio, _borderSize, _borderHexColor);
            }
        }

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            ToRounded(source, (int)_radius, _cropWidthRatio, _cropHeightRatio, _borderSize, _borderHexColor);

            return source;
        }

        public static void ToRounded(BitmapHolder source, int rad, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
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

            //TODO draws a border - we should optimize that and add some anti-aliasing
            if (borderSize > 0d)
            {
                borderSize = (borderSize * (desiredWidth + desiredHeight) / 2d / 1000d);
                int borderColor = Colors.Transparent.ToInt();

                try
                {
                    if (!borderHexColor.StartsWith("#", StringComparison.Ordinal))
                        borderHexColor.Insert(0, "#");
                    borderColor = borderHexColor.ToColorFromHex().ToInt();
                }
                catch (Exception)
                {
                }

                int intBorderSize = (int)Math.Ceiling(borderSize);

                for (int i = 0; i < intBorderSize; i++)
                {
                    DrawEllipse(source, i, i, 
                        ((int)Math.Floor(desiredWidth) - i), 
                        ((int)Math.Floor(desiredHeight) - i), 
                        borderColor);
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

        // https://github.com/teichgraf/WriteableBitmapEx/blob/master/Source/WriteableBitmapEx/WriteableBitmapShapeExtensions.cs

        /// <summary>
        /// A Fast Bresenham Type Algorithm For Drawing Ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf 
        /// x2 has to be greater than x1 and y2 has to be greater than y1.
        /// </summary>
        /// <param name="source">The BitmapHolder.</param>
        /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
        /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
        /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
        /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
        /// <param name="color">The color for the line.</param>
        public static void DrawEllipse(BitmapHolder source, int x1, int y1, int x2, int y2, int color)
        {
            // Calc center and radius
            int xr = (x2 - x1) >> 1;
            int yr = (y2 - y1) >> 1;
            int xc = x1 + xr;
            int yc = y1 + yr;
            DrawEllipseCentered(source, xc, yc, xr, yr, color);
        }

        /// <summary>
        /// A Fast Bresenham Type Algorithm For Drawing Ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf 
        /// Uses a different parameter representation than DrawEllipse().
        /// </summary>
        /// <param name="source">The BitmapHolder.</param>
        /// <param name="xc">The x-coordinate of the ellipses center.</param>
        /// <param name="yc">The y-coordinate of the ellipses center.</param>
        /// <param name="xr">The radius of the ellipse in x-direction.</param>
        /// <param name="yr">The radius of the ellipse in y-direction.</param>
        /// <param name="color">The color for the line.</param>
        public static void DrawEllipseCentered(BitmapHolder source, int xc, int yc, int xr, int yr, int color)
        {
            var pixels = source.Pixels;
            var w = source.Width;
            var h = source.Height;

            // Avoid endless loop
            if (xr < 1 || yr < 1)
            {
                return;
            }

            // Init vars
            int uh, lh, uy, ly, lx, rx;
            int x = xr;
            int y = 0;
            int xrSqTwo = (xr * xr) << 1;
            int yrSqTwo = (yr * yr) << 1;
            int xChg = yr * yr * (1 - (xr << 1));
            int yChg = xr * xr;
            int err = 0;
            int xStopping = yrSqTwo * xr;
            int yStopping = 0;

            // Draw first set of points counter clockwise where tangent line slope > -1.
            while (xStopping >= yStopping)
            {
                // Draw 4 quadrant points at once
                uy = yc + y;                  // Upper half
                ly = yc - y;                  // Lower half
                if (uy < 0) uy = 0;          // Clip
                if (uy >= h) uy = h - 1;      // ...
                if (ly < 0) ly = 0;
                if (ly >= h) ly = h - 1;
                uh = uy * w;                  // Upper half
                lh = ly * w;                  // Lower half

                rx = xc + x;
                lx = xc - x;
                if (rx < 0) rx = 0;          // Clip
                if (rx >= w) rx = w - 1;      // ...
                if (lx < 0) lx = 0;
                if (lx >= w) lx = w - 1;
                pixels[rx + uh] = color;      // Quadrant I (Actually an octant)
                pixels[lx + uh] = color;      // Quadrant II
                pixels[lx + lh] = color;      // Quadrant III
                pixels[rx + lh] = color;      // Quadrant IV

                y++;
                yStopping += xrSqTwo;
                err += yChg;
                yChg += xrSqTwo;
                if ((xChg + (err << 1)) > 0)
                {
                    x--;
                    xStopping -= yrSqTwo;
                    err += xChg;
                    xChg += yrSqTwo;
                }
            }

            // ReInit vars
            x = 0;
            y = yr;
            uy = yc + y;                  // Upper half
            ly = yc - y;                  // Lower half
            if (uy < 0) uy = 0;          // Clip
            if (uy >= h) uy = h - 1;      // ...
            if (ly < 0) ly = 0;
            if (ly >= h) ly = h - 1;
            uh = uy * w;                  // Upper half
            lh = ly * w;                  // Lower half
            xChg = yr * yr;
            yChg = xr * xr * (1 - (yr << 1));
            err = 0;
            xStopping = 0;
            yStopping = xrSqTwo * yr;

            // Draw second set of points clockwise where tangent line slope < -1.
            while (xStopping <= yStopping)
            {
                // Draw 4 quadrant points at once
                rx = xc + x;
                lx = xc - x;
                if (rx < 0) rx = 0;          // Clip
                if (rx >= w) rx = w - 1;      // ...
                if (lx < 0) lx = 0;
                if (lx >= w) lx = w - 1;
                pixels[rx + uh] = color;      // Quadrant I (Actually an octant)
                pixels[lx + uh] = color;      // Quadrant II
                pixels[lx + lh] = color;      // Quadrant III
                pixels[rx + lh] = color;      // Quadrant IV

                x++;
                xStopping += yrSqTwo;
                err += xChg;
                xChg += yrSqTwo;
                if ((yChg + (err << 1)) > 0)
                {
                    y--;
                    uy = yc + y;                  // Upper half
                    ly = yc - y;                  // Lower half
                    if (uy < 0) uy = 0;          // Clip
                    if (uy >= h) uy = h - 1;      // ...
                    if (ly < 0) ly = 0;
                    if (ly >= h) ly = h - 1;
                    uh = uy * w;                  // Upper half
                    lh = ly * w;                  // Lower half
                    yStopping -= xrSqTwo;
                    err += yChg;
                    yChg += xrSqTwo;
                }
            }
        }
    }
}
