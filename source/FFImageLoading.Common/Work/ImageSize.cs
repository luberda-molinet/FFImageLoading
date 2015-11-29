using System;

namespace FFImageLoading.Work
{
	public struct ImageSize
	{
		private int _width;
		private int _height;

		public ImageSize(int width, int height)
		{
			_width = width;
			_height = height;
		}

		public int Width
		{
			get
			{
				return _width;
			}
		}

		public int Height
		{
			get
			{
				return _height;
			}
		}
	}
}

