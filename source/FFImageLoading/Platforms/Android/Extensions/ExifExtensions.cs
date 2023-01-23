using System;
using Android.Graphics;
using FFImageLoading.Helpers.Exif;

namespace FFImageLoading.Extensions
{
    public static class ExifExtensions
    {
		public static Bitmap ToRotatedBitmap(this Bitmap sourceBitmap, ExifOrientation orientation)
        {
            if (orientation == ExifOrientation.ORIENTATION_UNDEFINED || orientation == ExifOrientation.ORIENTATION_NORMAL)
                return sourceBitmap;

			var width = sourceBitmap.Width;
			var height = sourceBitmap.Height;

			try
			{
				using (var matrix = new Matrix())
				{
					switch (orientation)
					{
						case ExifOrientation.ORIENTATION_FLIP_HORIZONTAL:
							matrix.PostScale(-1, 1);
							break;
						case ExifOrientation.ORIENTATION_ROTATE_180:
							matrix.PostRotate(180);
							break;
						case ExifOrientation.ORIENTATION_FLIP_VERTICAL:
							matrix.PostRotate(180);
							matrix.PostScale(-1, 1);
							break;
						case ExifOrientation.ORIENTATION_TRANSPOSE:
							matrix.PostRotate(90);
							matrix.PostScale(-1, 1);
							break;
						case ExifOrientation.ORIENTATION_ROTATE_90:
							matrix.PostRotate(90);
							break;
						case ExifOrientation.ORIENTATION_TRANSVERSE:
							matrix.PostRotate(270);
							matrix.PostScale(-1, 1);
							break;
						case ExifOrientation.ORIENTATION_ROTATE_270:
							matrix.PostRotate(270);
							break;
					}

					return Bitmap.CreateBitmap(sourceBitmap, 0, 0, width, height, matrix, false);
				}
			}
			finally
			{
				if (sourceBitmap != null && sourceBitmap.Handle != IntPtr.Zero && !sourceBitmap.IsRecycled)
				{
					sourceBitmap.Recycle();
					sourceBitmap.TryDispose();
				}
			}
		}
    }
}

