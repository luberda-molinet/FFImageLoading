using FFImageLoading.Extensions;
using FFImageLoading.Helpers;
using System;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Work
{
    public class BitmapHolder : IBitmap
    {
        public BitmapHolder(WriteableBitmap bitmap)
        {
            WriteableBitmap = bitmap;
        }

        public BitmapHolder(byte[] pixels, int width, int height)
        {
            PixelData = pixels;
            Width = width;
            Height = height;
        }

        public bool HasWriteableBitmap => WriteableBitmap != null;

        public WriteableBitmap WriteableBitmap { get; private set; }

        public int Height { get; private set; }

        public int Width { get; private set; }

        public byte[] PixelData { get; private set; }

        public int PixelCount { get { return (int)(PixelData.Length / 4); } }

        public void SetPixel(int x, int y, ColorHolder color)
        {
            int pixelPos = (y * Width + x);
            SetPixel(pixelPos, color);
        }

        public void SetPixel(int pos, ColorHolder color)
        {
            int bytePos = pos * 4;
            PixelData[bytePos] = color.B;
            PixelData[bytePos + 1] = color.G;
            PixelData[bytePos + 2] = color.R;
            PixelData[bytePos + 3] = color.A;
        }

        public ColorHolder GetPixel(int pos)
        {
            int bytePos = pos * 4;
            var b = PixelData[bytePos];
            var g = PixelData[bytePos + 1];
            var r = PixelData[bytePos + 2];
            var a = PixelData[bytePos + 3];

            return new ColorHolder(a, r, g, b);
        }

        public ColorHolder GetPixel(int x, int y)
        {
            int pixelPos = (y * Width + x);
            return GetPixel(pixelPos);
        }

        public void FreePixels()
        {
            PixelData = null;
            WriteableBitmap = null;
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
