using System;

namespace FFImageLoading.Work
{
	public enum LoadingResult
	{
		MemoryCache = 1,
		DiskCache = 2,
		Internet = 3,

		Disk = 10,
		ApplicationBundle = 11,
		CompiledResource = 12,

		Stream = 20,
	}
}

