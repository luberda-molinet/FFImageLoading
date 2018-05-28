using System;
using Android.Graphics.Drawables;
using FFImageLoading.Drawables;
using System.IO;
using System.Threading.Tasks;

namespace FFImageLoading
{
    public static class DrawableExtensions
    {
        public static bool IsValidAndHasValidBitmap(this BitmapDrawable drawable)
        {
            var sdDrawable = drawable as ISelfDisposingBitmapDrawable;
            if (sdDrawable != null)
            {
                return drawable != null && drawable.Handle != IntPtr.Zero && sdDrawable.HasValidBitmap;
            }

            return drawable != null && drawable.Handle != IntPtr.Zero && drawable.Bitmap != null && drawable.Bitmap.Handle != IntPtr.Zero && !drawable.Bitmap.IsRecycled;
        }

        public static bool IsValidAndHasValidBitmap(this ISelfDisposingBitmapDrawable drawable)
        {
            return drawable != null && drawable.Handle != IntPtr.Zero && drawable.HasValidBitmap;
        }

        public static bool IsValidAndHasValidBitmap(this SelfDisposingBitmapDrawable drawable)
        {
            return drawable != null && drawable.Handle != IntPtr.Zero && drawable.HasValidBitmap;
        }

        public static async Task<Stream> AsPngStreamAsync(this BitmapDrawable drawable)
        {
            var stream = new MemoryStream();
            await drawable.Bitmap.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);

            if (stream.Position != 0)
                stream.Position = 0;

            return stream;
        }

        public static async Task<Stream> AsJpegStreamAsync(this BitmapDrawable drawable, int quality = 90)
        {
            var stream = new MemoryStream();
            await drawable.Bitmap.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Jpeg, quality, stream);

            if (stream.Position != 0)
                stream.Position = 0;

            return stream;
        }
    }
}
