using System;

namespace FFImageLoading.Work
{
	public enum LoadingResult
	{
		// Errors
		NotFound = -1,
		InvalidTarget = -2,
		Canceled = -3,
		Failed = -4,

		// Success results
		MemoryCache = 1,
		DiskCache = 2,
		Internet = 3,

		Disk = 10,
		ApplicationBundle = 11,
		CompiledResource = 12,
        EmbeddedResource = 13,

		Stream = 20,
	}
}

