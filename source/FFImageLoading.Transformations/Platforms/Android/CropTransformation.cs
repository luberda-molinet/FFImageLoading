using Android.Graphics;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
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

        protected override Bitmap Transform(Bitmap sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToCropped(sourceBitmap, ZoomFactor, XOffset, YOffset, CropWidthRatio, CropHeightRatio);
        }

        public static Bitmap ToCropped(Bitmap source, double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
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

            desiredWidth =  desiredWidth / zoomFactor;
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

            var config = source.GetConfig();
            if (config == null)
                config = Bitmap.Config.Argb8888;    // This will support transparency

            Bitmap bitmap = Bitmap.CreateBitmap((int)desiredWidth, (int)desiredHeight, config);

            using (Canvas canvas = new Canvas(bitmap))
            using (Paint paint = new Paint())
            using (BitmapShader shader = new BitmapShader(source, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
            using (Matrix matrix = new Matrix())
            {
                if (cropX != 0 || cropY != 0)
                {
                    matrix.SetTranslate(-cropX, -cropY);
                    shader.SetLocalMatrix(matrix);
                }

                paint.SetShader(shader);
                paint.AntiAlias = false;

                RectF rectF = new RectF(0, 0, (int)desiredWidth, (int)desiredHeight);
                canvas.DrawRect(rectF, paint);

                return bitmap;
            }
        }
    }
}

