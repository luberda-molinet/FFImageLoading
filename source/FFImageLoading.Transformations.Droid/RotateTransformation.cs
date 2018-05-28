using System;
using Android.Graphics;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RotateTransformation : TransformationBase
    {
        public RotateTransformation() : this(30d)
        {
        }

        public RotateTransformation(double degrees) : this(degrees, false, false)
        {
        }

        public RotateTransformation(double degrees, bool ccw) : this(degrees, ccw, false)
        {
        }

        public RotateTransformation(double degrees, bool ccw, bool resize)
        {
            Degrees = degrees;
            CCW = ccw;
            Resize = resize;
        }

        public double Degrees { get; set; }
        public bool CCW { get; set; }
        public bool Resize { get; set; }

        public override string Key
        {
            get { return string.Format("RotateTransformation,degrees={0},ccw={1},resize={2}", Degrees, CCW, Resize); }
        }

        protected override Bitmap Transform(Bitmap sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToRotated(sourceBitmap, Degrees, CCW, Resize);
        }

        public static Bitmap ToRotated(Bitmap source, double degrees, bool ccw, bool resize)
        {
            if (degrees == 0 || degrees % 360 == 0)
                return source;

            if (ccw)
                degrees = 360d - degrees;

            Bitmap bitmap = Bitmap.CreateBitmap(source.Width, source.Height, Bitmap.Config.Argb8888);
            bitmap.HasAlpha = true;
            using (Canvas canvas = new Canvas(bitmap))
            using (Paint paint = new Paint())
            using (BitmapShader shader = new BitmapShader(source, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
            using (Matrix matrix = new Matrix())
            {
                paint.AntiAlias = true;
                paint.Dither = true;
                paint.FilterBitmap = true;

                float targetRotation = (float)degrees;
                float rotationPivotX = (float)source.Width / 2.0f;
                float rotationPivotY = (float)source.Height / 2.0f;
                float targetWidth = source.Width;
                float targetHeight = source.Height;

                if (resize && (degrees % 180 != 0))
                {
                    double cosR = Math.Cos(DegreesToRadians(targetRotation));
                    double sinR = Math.Sin(DegreesToRadians(targetRotation));

                    // Recalculate dimensions after rotation around pivot point
                    double x1T = rotationPivotX * (1.0 - cosR) + (rotationPivotY * sinR);
                    double y1T = rotationPivotY * (1.0 - cosR) - (rotationPivotX * sinR);
                    double x2T = x1T + (targetWidth * cosR);
                    double y2T = y1T + (targetWidth * sinR);
                    double x3T = x1T + (targetWidth * cosR) - (targetHeight * sinR);
                    double y3T = y1T + (targetWidth * sinR) + (targetHeight * cosR);
                    double x4T = x1T - (targetHeight * sinR);
                    double y4T = y1T + (targetHeight * cosR);

                    double maxX = Math.Max(x4T, Math.Max(x3T, Math.Max(x1T, x2T)));
                    double minX = Math.Min(x4T, Math.Min(x3T, Math.Min(x1T, x2T)));
                    double maxY = Math.Max(y4T, Math.Max(y3T, Math.Max(y1T, y2T)));
                    double minY = Math.Min(y4T, Math.Min(y3T, Math.Min(y1T, y2T)));
                    targetWidth = (int) Math.Floor(maxX - minX);
                    targetHeight  = (int) Math.Floor(maxY - minY);

                    float sx = (float)source.Width / targetWidth;
                    float sy = (float)source.Height / targetHeight;

                    matrix.SetScale(sx, sy, rotationPivotX, rotationPivotY);
                }

                matrix.PostRotate(targetRotation, rotationPivotX, rotationPivotY);
                canvas.DrawBitmap(source, matrix, paint);

                return bitmap;
            }
        }

        private static double RadiansToDegrees(double angle)
        {
            return angle * (180.0d / Math.PI);
        }

        private static double DegreesToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}

