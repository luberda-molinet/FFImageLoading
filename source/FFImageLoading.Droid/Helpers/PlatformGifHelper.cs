using System;
using System.Threading.Tasks;
using Android.Graphics;
using FFImageLoading.Extensions;

namespace FFImageLoading.Helpers
{
    internal class PlatformGifHelper : GifHelperBase<Bitmap>
    {
        protected override int DipToPixels(int dips)
        {
            return dips.DpToPixels();
        }

        protected override Task<Bitmap> ToBitmapAsync(int[] data, int width, int height)
        {
            var bitmap = Bitmap.CreateBitmap(data, width, height, Bitmap.Config.Argb4444);
            return Task.FromResult(bitmap);
        }

    }
}
