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
            try
            {
                if (drawable is ISelfDisposingBitmapDrawable sdDrawable)
                {
                    return drawable != null && drawable.Handle != IntPtr.Zero && sdDrawable.HasValidBitmap;
                }

                return drawable != null && drawable.Handle != IntPtr.Zero && drawable.Bitmap != null && drawable.Bitmap.Handle != IntPtr.Zero && !drawable.Bitmap.IsRecycled;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public static bool IsValidAndHasValidBitmap(this ISelfDisposingBitmapDrawable drawable)
        {
            try
            {
                return drawable != null && drawable.Handle != IntPtr.Zero && drawable.HasValidBitmap;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public static bool IsValidAndHasValidBitmap(this SelfDisposingBitmapDrawable drawable)
        {
            try
            {
                return drawable != null && drawable.Handle != IntPtr.Zero && drawable.HasValidBitmap;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public static async Task<Stream> AsPngStreamAsync(this BitmapDrawable drawable)
        {
            var sdbd = drawable as ISelfDisposingBitmapDrawable;
            sdbd?.SetIsRetained(true);

            try
            {
                var stream = new MemoryStream();
                await drawable.Bitmap.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Png, 100, stream).ConfigureAwait(false);

                if (stream.Position != 0)
                    stream.Position = 0;


                return stream;
            }
            finally
            {
                sdbd?.SetIsRetained(false);
            }
        }

        public static async Task<Stream> AsJpegStreamAsync(this BitmapDrawable drawable, int quality = 90)
        {
            var sdbd = drawable as ISelfDisposingBitmapDrawable;
            sdbd?.SetIsRetained(true);

            try
            {
                var stream = new MemoryStream();
                await drawable.Bitmap.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Jpeg, quality, stream).ConfigureAwait(false);

                if (stream.Position != 0)
                    stream.Position = 0;

                return stream;
            }
            finally
            {
                sdbd?.SetIsRetained(false);
            }
        }
    }
}
