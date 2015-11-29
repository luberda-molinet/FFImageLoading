using System;
using UIKit;

namespace FFImageLoading.Work
{
	public class BitmapHolder: IBitmap
	{
		public BitmapHolder(UIImage bitmap)
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

		internal UIImage NativeBitmap
		{
			get;
			private set;
		}
	}

	public static class IBitmapExtensions
	{
		public static UIImage ToNative(this IBitmap bitmap)
		{
			return ((BitmapHolder)bitmap).NativeBitmap;
		}
	}
}

