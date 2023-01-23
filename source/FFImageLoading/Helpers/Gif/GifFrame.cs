using System;

namespace FFImageLoading.Helpers.Gif
{
	public class GifFrame
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public bool Interlace { get; set; }
		public bool Transparency { get; set; }
		public Disposal Dispose { get; set; }
		public int TransparencyIndex { get; set; }
		public int Delay { get; set; }
		public int BufferFrameStart { get; set; }
  		public int[] LCT { get; set; }

		public enum Disposal
		{
			UNSPECIFIED = 0,
			NONE = 1,
			BACKGROUND = 2,
			PREVIOUS = 3,
		}
	}
}
