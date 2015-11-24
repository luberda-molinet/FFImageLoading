using System;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Work
{
    public class BitmapHolder : IBitmap
    {
        public BitmapHolder(int[] pixels, int width, int height)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
        }

        public int Height
        {
            get; private set; 
        }

        public int Width
        {
            get; private set; 
        }

        public int[] Pixels { get; private set; }
    }

    public static class IBitmapExtensions
    {
        public static BitmapHolder ToNative(this IBitmap bitmap)
        {
            return (BitmapHolder)bitmap;
        }
    }
}
