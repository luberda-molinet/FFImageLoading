using System;
using Windows.UI;
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

        internal void SetPixels(int[] pixels, int width, int height)
        {
            Pixels = null;
            Pixels = pixels;
            Width = width;
            Height = height;
        }

        public void SetPixel(int x, int y, int color)
        {
            if (x < Width && y < Height)
                Pixels[y * Width + x] = color;
        }

        public void SetPixel(int x, int y, Color color)
        {
            SetPixel(x, y, ToInt(color));
        }

		public void FreePixels()
		{
			Pixels = null;
		}

        static int ToInt(Color color)
        {
            var col = 0;

            if (color.A != 0)
            {
                var a = color.A + 1;
                col = (color.A << 24)
                  | ((byte)((color.R * a) >> 8) << 16)
                  | ((byte)((color.G * a) >> 8) << 8)
                  | ((byte)((color.B * a) >> 8));
            }

            return col;
        }
    }

    public static class IBitmapExtensions
    {
        public static BitmapHolder ToNative(this IBitmap bitmap)
        {
            return (BitmapHolder)bitmap;
        }
    }
}
