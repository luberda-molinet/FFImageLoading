using System;
using System.Collections.Generic;

namespace FFImageLoading.Helpers.Gif
{
	public class GifHeader
	{
		public int[] GCT { get; set; }

		public enum LoopCountType
		{
			NETSCAPE_LOOP_COUNT_DOES_NOT_EXIST = -1,
			NETSCAPE_LOOP_COUNT_FOREVER = 0,
		}

		public IList<GifFrame> Frames { get; set; } = new List<GifFrame>();
		public GifFrame CurrentFrame { get; set; }
		public int FrameCount { get; set; }
		public bool GCTFlag { get; set; }
		public int GCTSize { get; set; }
		public int PixelAspect { get; set; }
		public int BackgroundColor { get; set; }
		public int BackgroundIndex { get; set; }
		public int LoopCount { get; set; } = (int)LoopCountType.NETSCAPE_LOOP_COUNT_DOES_NOT_EXIST;
		public int Height { get; set; }
		public int Width { get; set; }
		public int NumFrames { get; set; }
		public GifDecodeStatus Status { get; set; }
	}
}
