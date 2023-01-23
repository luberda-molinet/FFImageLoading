using System;
using Android.Graphics;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RoundedTransformation : TransformationBase
    {
        public RoundedTransformation() : this(30d)
        {
        }

        public RoundedTransformation(double radius) : this(radius, 1d, 1d)
        {
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio) : this(radius, cropWidthRatio, cropHeightRatio, 0d, null)
        {
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
        {
            Radius = radius;
            CropWidthRatio = cropWidthRatio;
            CropHeightRatio = cropHeightRatio;
            BorderSize = borderSize;
            BorderHexColor = borderHexColor;
        }

        public double Radius { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }
        public double BorderSize { get; set; }
        public string BorderHexColor { get; set; }

        public override string Key
        {
            get
            {
                return string.Format("RoundedTransformation,radius={0},cropWidthRatio={1},cropHeightRatio={2},borderSize={3},borderHexColor={4}",
                Radius, CropWidthRatio, CropHeightRatio, BorderSize, BorderHexColor);
            }
        }

        protected override Bitmap Transform(Bitmap sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToRounded(sourceBitmap, (float)Radius, CropWidthRatio, CropHeightRatio, BorderSize, BorderHexColor);
        }

        public static Bitmap ToRounded(Bitmap source, float rad, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
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

            float cropX = (float)((sourceWidth - desiredWidth) / 2d);
            float cropY = (float)((sourceHeight - desiredHeight) / 2d);

            if (rad == 0)
                rad = (float)(Math.Min(desiredWidth, desiredHeight) / 2d);
            else
                rad = (float)(rad * (desiredWidth + desiredHeight) / 2d / 500d);

            Bitmap bitmap = Bitmap.CreateBitmap((int)desiredWidth, (int)desiredHeight, Bitmap.Config.Argb8888);
            bitmap.HasAlpha = true;

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
                paint.AntiAlias = true;

                RectF rectF = new RectF(0f, 0f, (float)desiredWidth, (float)desiredHeight);
                canvas.DrawRoundRect(rectF, rad, rad, paint);

                if (borderSize > 0d)
                {
                    borderSize = (borderSize * (desiredWidth + desiredHeight) / 2d / 500d);
                    paint.Color = borderHexColor.ToColor(); ;
                    paint.SetStyle(Paint.Style.Stroke);
                    paint.StrokeWidth = (float)borderSize;
                    paint.SetShader(null);

                    RectF borderRectF = new RectF((float)(0d + borderSize/2d), (float)(0d + borderSize/2d),
                        (float)(desiredWidth - borderSize/2d), (float)(desiredHeight - borderSize/2d));

                    canvas.DrawRoundRect(borderRectF, rad, rad, paint);
                }

                return bitmap;
            }
        }
    }
}

