using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class CropTransformation : TransformationBase
    {
        private double _zoomFactor;
        private double _xOffset;
        private double _yOffset;
        private double _cropWidthRatio;
        private double _cropHeightRatio;

        public CropTransformation(double zoomFactor, double xOffset, double yOffset) : this(zoomFactor, xOffset, yOffset, 1f, 1f)
        {
        }

        public CropTransformation(double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
        {
            _zoomFactor = zoomFactor;
            _xOffset = xOffset;
            _yOffset = yOffset;
            _cropWidthRatio = cropWidthRatio;
            _cropHeightRatio = cropHeightRatio;

            if (zoomFactor < 1f)
                _zoomFactor = 1f;
        }

        public override string Key
        {
            get
            {
                return string.Format("CropTransformation,zoomFactor={0},xOffset={1},yOffset={2},cropWidthRatio={3},cropHeightRatio={4}",
              _zoomFactor, _xOffset, _yOffset, _cropWidthRatio, _cropHeightRatio);
            }
        }

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            ToCropped(source, _zoomFactor, _xOffset, _yOffset, _cropWidthRatio, _cropHeightRatio);

            return source;
        }

        public static void ToCropped(BitmapHolder source, double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
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

            xOffset = xOffset * desiredWidth;
            yOffset = yOffset * desiredHeight;

            desiredWidth = desiredWidth / zoomFactor;
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

            int width = (int)desiredWidth;
            int height = (int)desiredHeight;

            // Copy the pixels line by line using fast BlockCopy
            var result = new int[width * height];

            for (var line = 0; line < height; line++)
            {
                var srcOff = (((int)cropY + line) * source.Width + (int)cropX) * Helpers.SizeOfArgb;
                var dstOff = line * width * Helpers.SizeOfArgb;
                Helpers.BlockCopy(source.Pixels, srcOff, result, dstOff, width * Helpers.SizeOfArgb);
            }

            source.SetPixels(result, width, height);
        }

        public static void ToCropped(BitmapHolder source, int x, int y, int width, int height)
        {
            var srcWidth = source.Width;
            var srcHeight = source.Height;

            // If the rectangle is completely out of the bitmap
            if (x > srcWidth || y > srcHeight)
            {
                return;
            }

            // Clamp to boundaries
            if (x < 0) x = 0;
            if (x + width > srcWidth) width = srcWidth - x;
            if (y < 0) y = 0;
            if (y + height > srcHeight) height = srcHeight - y;

            // Copy the pixels line by line using fast BlockCopy
            var result = new int[width * height];

            for (var line = 0; line < height; line++)
            {
                var srcOff = ((y + line) * srcWidth + x) * Helpers.SizeOfArgb;
                var dstOff = line * width * Helpers.SizeOfArgb;
                Helpers.BlockCopy(source.Pixels, srcOff, result, dstOff, width * Helpers.SizeOfArgb);
            }

            source.SetPixels(result, width, height);
        }
    }
}
