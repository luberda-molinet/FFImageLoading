using System;
using Android.Graphics;
using FFImageLoading.Work;
using Android.Media;

namespace FFImageLoading.Extensions
{
	public static class ExifExtensions
	{
		public static Bitmap ToExifRotatedBitmap(this Bitmap sourceBitmap, ImageSource source, string identifier)
		{
			if (source == ImageSource.Filepath)
			{
				int rotation = 0;
				var exifInt = new ExifInterface(identifier);
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
				}

				if (rotation == 0)
					return sourceBitmap;

				int width = sourceBitmap.Width;
				int height = sourceBitmap.Height;

				if (rotation == 90 || rotation == 270) 
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

					matrix.PostRotate(rotation);
					canvas.DrawBitmap(sourceBitmap, matrix, paint);
				}

				if (sourceBitmap != null && sourceBitmap.Handle != IntPtr.Zero && !sourceBitmap.IsRecycled)
				{
					sourceBitmap.Recycle();
					sourceBitmap.Dispose();	
				}

				return bitmap;
			}
			
			return sourceBitmap;
		}
	}
}

