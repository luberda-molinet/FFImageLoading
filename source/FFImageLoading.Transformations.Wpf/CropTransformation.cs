using FFImageLoading.Extensions;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
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

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToCropped(bitmapSource, ZoomFactor, XOffset, YOffset, CropWidthRatio, CropHeightRatio);
        }

        public static BitmapHolder ToCropped(BitmapHolder source, double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
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
            var result = new byte[width * height * 4];

            for (var line = 0; line < height; line++)
            {
                var srcOff = (((int)cropY + line) * source.Width + (int)cropX) * ColorExtensions.SizeOfArgb;
                var dstOff = line * width * ColorExtensions.SizeOfArgb;
                Helpers.BlockCopy(source.PixelData, srcOff, result, dstOff, width * ColorExtensions.SizeOfArgb);
            }

            return new BitmapHolder(result, width, height);
        }

        public static BitmapHolder ToCropped(BitmapHolder source, int x, int y, int width, int height)
        {
            var srcWidth = source.Width;
            var srcHeight = source.Height;

            // Clamp to boundaries
            if (x < 0) x = 0;
            if (x + width > srcWidth) width = srcWidth - x;
            if (y < 0) y = 0;
            if (y + height > srcHeight) height = srcHeight - y;

            // Copy the pixels line by line using fast BlockCopy
            var result = new byte[width * height * 4];

            for (var line = 0; line < height; line++)
            {
                var srcOff = ((y + line) * srcWidth + x) * ColorExtensions.SizeOfArgb;
                var dstOff = line * width * ColorExtensions.SizeOfArgb;
                Helpers.BlockCopy(source.PixelData, srcOff, result, dstOff, width * ColorExtensions.SizeOfArgb);
            }

            return new BitmapHolder(result, width, height);
        }
    }
}
