using System;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Work
{
    public class BitmapHolder : IBitmap
    {
        public BitmapHolder(WriteableBitmap bitmap)
        {
            NativeBitmap = bitmap;
        }

        public int Height
        {
            get
            {
                return NativeBitmap.PixelHeight;
            }
        }

        public int Width
        {
            get
            {
                return NativeBitmap.PixelWidth;
            }
        }

        internal WriteableBitmap NativeBitmap { get; private set; }
    }

    public static class IBitmapExtensions
    {
        public static WriteableBitmap ToNative(this IBitmap bitmap)
        {
            return ((BitmapHolder)bitmap).NativeBitmap;
        }
    }
}
