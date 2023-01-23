using System;
using FFImageLoading.Work;
using ElmSharp;
using FFImageLoading.Views;

namespace FFImageLoading
{
    public class BitmapHolder : IBitmap
    {
        public BitmapHolder(SharedEvasImage bitmap)
        {
            NativeBitmap = bitmap;
        }

        public int Width
        {
            get
            {
                return (int)NativeBitmap.Size.Width;
            }
        }

        public int Height
        {
            get
            {
                return (int)NativeBitmap.Size.Height;
            }
        }

        internal SharedEvasImage NativeBitmap
        {
            get;
            private set;
        }
    }

    public static class IBitmapExtensions
    {
        public static SharedEvasImage ToNative(this IBitmap bitmap)
        {
            return ((BitmapHolder)bitmap).NativeBitmap;
        }
    }
}
