using System;

namespace FFImageLoading.Work
{
	[Obsolete]
	public struct ImageSize
	{
		public ImageSize(int width, int height)
		{
			Width = width;
			Height = height;
		}
			
		public int Width { get; private set; }

		public int Height { get; private set; }
	}
}

