using FFImageLoading.Extensions;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
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

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToTransformedCorners(bitmapSource, TopLeftCornerSize, TopRightCornerSize, BottomLeftCornerSize, BottomRightCornerSize,
                CornersTransformType, CropWidthRatio, CropHeightRatio);
        }

        public static BitmapHolder ToTransformedCorners(BitmapHolder source, double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
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

            BitmapHolder bitmap = null;

            if (cropX != 0 || cropY != 0)
            {
                bitmap = CropTransformation.ToCropped(source, (int)cropX, (int)cropY, (int)(desiredWidth), (int)(desiredHeight));
            }
            else
            {
                bitmap = new BitmapHolder(source.PixelData, source.Width, source.Height);
            }

            topLeftCornerSize = topLeftCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            topRightCornerSize = topRightCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            bottomLeftCornerSize = bottomLeftCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            bottomRightCornerSize = bottomRightCornerSize * (desiredWidth + desiredHeight) / 2 / 100;

            int topLeftSize = (int)topLeftCornerSize;
            int topRightSize = (int)topRightCornerSize;
            int bottomLeftSize = (int)bottomLeftCornerSize;
            int bottomRightSize = (int)bottomRightCornerSize;

            int w = bitmap.Width;
            int h = bitmap.Height;

            var transparentColor = ColorHolder.Transparent;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x <= topLeftSize && y <= topLeftSize)
                    { //top left corner
                        if (!CheckCorner(topLeftSize, topLeftSize, topLeftSize, cornersTransformType, Corner.TopLeftCorner, x, y))
                            bitmap.SetPixel(y * w + x, transparentColor);
                    }
                    else if (x >= w - topRightSize && y <= topRightSize && topRightSize > 0)
                    { // top right corner
                        if (!CheckCorner(w - topRightSize, topRightSize, topRightSize, cornersTransformType, Corner.TopRightCorner, x, y))
                            bitmap.SetPixel(y * w + x, transparentColor);
                    }
                    else if (x >= w - bottomRightSize && y >= h - bottomRightSize && bottomRightSize > 0)
                    { // bottom right corner
                        if (!CheckCorner(w - bottomRightSize, h - bottomRightSize, bottomRightSize, cornersTransformType, Corner.BottomRightCorner, x, y))
                            bitmap.SetPixel(y * w + x, transparentColor);
                    }
                    else if (x <= bottomLeftSize && y >= h - bottomLeftSize && bottomLeftSize > 0)
                    { // bottom left corner
                        if (!CheckCorner(bottomLeftSize, h - bottomLeftSize, bottomLeftSize, cornersTransformType, Corner.BottomLeftCorner, x, y))
                            bitmap.SetPixel(y * w + x, transparentColor);
                    }
                }
            }

            return bitmap;
        }

        private enum Corner
        {
            TopLeftCorner,
            TopRightCorner,
            BottomRightCorner,
            BottomLeftCorner,
        }

        private static bool HasFlag(CornerTransformType flags, CornerTransformType flag)
        {
            return (flags & flag) != 0;
        }

        private static bool CheckCorner(int w, int h, int size, CornerTransformType flags, Corner which, int xC, int yC)
        {
            if ((HasFlag(flags, CornerTransformType.TopLeftCut) && which == Corner.TopLeftCorner)
                || (HasFlag(flags, CornerTransformType.TopRightCut) && which == Corner.TopRightCorner)
                || (HasFlag(flags, CornerTransformType.BottomRightCut) && which == Corner.BottomRightCorner)
                || (HasFlag(flags, CornerTransformType.BottomLeftCut) && which == Corner.BottomLeftCorner))
                return CheckCutCorner(w, h, size, which, xC, yC);

            if ((HasFlag(flags, CornerTransformType.TopLeftRounded) && which == Corner.TopLeftCorner)
                || (HasFlag(flags, CornerTransformType.TopRightRounded) && which == Corner.TopRightCorner)
                || (HasFlag(flags, CornerTransformType.BottomRightRounded) && which == Corner.BottomRightCorner)
                || (HasFlag(flags, CornerTransformType.BottomLeftRounded) && which == Corner.BottomLeftCorner))
                return CheckRoundedCorner(w, h, size, which, xC, yC);

            return true;
        }

        private static bool CheckCutCorner(int w, int h, int size, Corner which, int xC, int yC)
        {
            switch (which)
            {
                case Corner.TopLeftCorner:
                    {   //Testing if its outside the top left corner
                        return Slope(size, 0, xC-1, yC) < Slope(size, 0, 0, size);
                    }
                case Corner.TopRightCorner:
                    {   //Testing if its outside the top right corner
                        return Slope(w, 0, xC, yC) > Slope(w, 0, w+size, size);
                    }
                case Corner.BottomRightCorner:
                    {   //Testing if its outside the bottom right corner
                        return Slope(h+size, h, xC, yC) > Slope(h+size, h, w, h+size);
                    }
                case Corner.BottomLeftCorner:
                    {   //Testing if its outside the bottom left corner
                        return Slope(0, h, xC, yC) < Slope(0, h, size, h+size);
                    }
            }

            return true;
        }

        private static double Slope(double x1, double y1, double x2, double y2)
        {
            return (y2 - y1) / (x2 - x1);
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
