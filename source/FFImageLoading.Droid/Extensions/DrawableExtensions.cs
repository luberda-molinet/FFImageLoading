using System;
using Android.Graphics.Drawables;
using FFImageLoading.Drawables;
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
    }
}
