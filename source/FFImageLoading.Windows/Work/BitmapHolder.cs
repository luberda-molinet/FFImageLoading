using FFImageLoading.Extensions;
using FFImageLoading.Helpers;
using System;
using Windows.UI;

namespace FFImageLoading.Work
{
    public class BitmapHolder : IBitmap
    {
        public BitmapHolder(byte[] pixels, int width, int height)
        {
            PixelData = pixels;
            Width = width;
            Height = height;
        }

        public int Height { get; private set; }

        public int Width { get; private set; }

        public byte[] PixelData { get; private set; }

        public int PixelCount { get { return (int)(PixelData.Length / 4); } }

        public void SetPixel(int x, int y, int color)
        {
            int pixelPos = (y * Width + x);
            SetPixel(pixelPos, color);
        }

        public unsafe void SetPixel(int pos, int color)
        {
            fixed (byte* numPtr = &PixelData[pos * 4])
            *(int*)numPtr = color;
        }

        public void SetPixel(int x, int y, ColorHolder color)
        {
            int pixelPos = (y * Width + x);
            SetPixel(pixelPos, color);
        }

        public void SetPixel(int pos, ColorHolder color)
        {
            int bytePos = pos * 4;
            PixelData[bytePos] = color.A;
            PixelData[bytePos + 1] = color.R;
            PixelData[bytePos + 2] = color.G;
            PixelData[bytePos + 3] = color.B;
        }

        public int GetPixelAsInt(int x, int y)
        {
            int pixelPos = (y * Width + x);
            return GetPixelAsInt(pixelPos);
        }

        public int GetPixelAsInt(int pos)
        {
            return BitConverter.ToInt32(PixelData, pos * 4);
        }

        public Color GetPixelAsColor(int x, int y)
        {
            int pixelPos = (y * Width + x) ;
            return GetPixelAsColor(pixelPos);
        }

        public Color GetPixelAsColor(int pos)
        {
            int bytePos = pos * 4;
            return Color.FromArgb(PixelData[bytePos], PixelData[bytePos + 1], PixelData[bytePos + 2], PixelData[bytePos + 3]);
        }

        public void FreePixels()
        {
            PixelData = null;
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
