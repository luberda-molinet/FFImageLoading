using System;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif

namespace FFImageLoading.Work
{
    public class BitmapHolder: IBitmap
    {
        public BitmapHolder(PImage bitmap)
        {
            NativeBitmap = bitmap;
        }

        public int Width => (int)NativeBitmap.Size.Width;

        public int Height => (int)NativeBitmap.Size.Height;

        internal PImage NativeBitmap
        {
            get;
            private set;
        }
    }

    public static class IBitmapExtensions
    {
        public static PImage ToNative(this IBitmap bitmap)
        {
            return ((BitmapHolder)bitmap).NativeBitmap;
        }
    }
}

