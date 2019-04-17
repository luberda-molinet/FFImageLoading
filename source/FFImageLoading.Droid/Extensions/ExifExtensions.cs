using System;
using Android.Graphics;
using System.IO;

namespace FFImageLoading.Extensions
{
    public static class ExifExtensions
    {
        //public static int GetExifRotationDegrees(this Stream stream)
        //{
        //  int rotation = 0;
        //
        //	var exifInt = new ExifInterface(stream);
        //
        //	int exifRotation = exifInt.GetAttributeInt(ExifInterface.TagOrientation, ExifInterface.OrientationNormal);
        //
        //	switch (exifRotation)
        //	{
        //		case ExifInterface.OrientationRotate270:
        //			rotation = 270;
        //			break;
        //		case ExifInterface.OrientationRotate180:
        //			rotation = 180;
        //			break;
        //		case ExifInterface.OrientationRotate90:
        //			rotation = 90;
        //			break;
        //		default:
        //          rotation = 0;
        //          break;
        //	}
        //	return rotation;
        //}

        public static Bitmap ToRotatedBitmap(this Bitmap sourceBitmap, int rotationDegrees)
        {
            if (rotationDegrees == 0)
                return sourceBitmap;

            var width = sourceBitmap.Width;
            var height = sourceBitmap.Height;

            if (rotationDegrees == 90 || rotationDegrees == 270)
            {
                width = sourceBitmap.Height;
                height = sourceBitmap.Width;
            }

            var bitmap = Bitmap.CreateBitmap(width, height, sourceBitmap.GetConfig());
            using (var canvas = new Canvas(bitmap))
            using (var paint = new Paint())
            using (var shader = new BitmapShader(sourceBitmap, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
            using (var matrix = new Matrix())
            {
                // paint.AntiAlias = true;
                // paint.Dither = true;
                // paint.FilterBitmap = true;
                canvas.Save();
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

