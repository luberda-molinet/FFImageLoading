using System;
namespace FFImageLoading.Helpers.Gif
{
	public enum GifDecodeStatus
	{
		STATUS_OK = 0,
		STATUS_FORMAT_ERROR = 1,
		STATUS_OPEN_ERROR = 2,
		STATUS_PARTIAL_DECODE = 3,
		TOTAL_ITERATION_COUNT_FOREVER = 0,
	}
}
