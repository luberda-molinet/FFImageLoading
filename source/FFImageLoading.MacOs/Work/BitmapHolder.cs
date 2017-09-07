using System;
using AppKit;

namespace FFImageLoading.Work
{
	public class BitmapHolder: IBitmap
	{
		public BitmapHolder(NSImage bitmap)
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

		internal NSImage NativeBitmap
		{
			get;
			private set;
		}
	}

	public static class IBitmapExtensions
	{
		public static NSImage ToNative(this IBitmap bitmap)
		{
			return ((BitmapHolder)bitmap).NativeBitmap;
		}
	}
}

