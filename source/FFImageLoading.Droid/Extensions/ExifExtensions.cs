using System;
using Android.Graphics;
using Android.Media;

namespace FFImageLoading.Extensions
{
    public static class ExifExtensions
    {
        public static int GetExifRotationDegrees(this string filePath)
        {
            int rotation = 0;
            var exifInt = new ExifInterface(filePath);
            int exifRotation = exifInt.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);

            switch (exifRotation)
            {
                case (int) Orientation.Rotate270:
                    rotation = 270;
                    break;
                case (int) Orientation.Rotate180:
                    rotation = 180;
                    break;
                case (int) Orientation.Rotate90:
                    rotation = 90;
                    break;
                default:
                    return 0;
            }

            return rotation;
        }

        public static Bitmap ToRotatedBitmap(this Bitmap sourceBitmap, int rotationDegrees)
        {
            if (rotationDegrees == 0)
                return sourceBitmap;

            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            if (rotationDegrees == 90 || rotationDegrees == 270)
            {
                width = sourceBitmap.Height;
                height = sourceBitmap.Width;
            }

            Bitmap bitmap = Bitmap.CreateBitmap(width, height, sourceBitmap.GetConfig());
            using (Canvas canvas = new Canvas(bitmap))
            using (Paint paint = new Paint())
            using (BitmapShader shader = new BitmapShader(sourceBitmap, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
            using (Matrix matrix = new Matrix())
            {
                // paint.AntiAlias = true;
                // paint.Dither = true;
                // paint.FilterBitmap = true;

                canvas.Save(Android.Graphics.SaveFlags.Matrix);

                if (rotationDegrees == 90)
                    canvas.Rotate(rotationDegrees, width / 2, width / 2);
                else if (rotationDegrees == 270)
                    canvas.Rotate(rotationDegrees, height / 2, height / 2);
                else
                    canvas.Rotate(rotationDegrees, width / 2, height / 2);

                canvas.DrawBitmap(sourceBitmap, matrix, paint);
                canvas.Restore();
            }

            if (sourceBitmap != null && sourceBitmap.Handle != IntPtr.Zero && !sourceBitmap.IsRecycled)
            {
                sourceBitmap.Recycle();
                sourceBitmap.TryDispose();
            }

            return bitmap;
        }
    }
}

