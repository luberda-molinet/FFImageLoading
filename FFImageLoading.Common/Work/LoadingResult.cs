using System;

namespace FFImageLoading.Work
{
	public enum LoadingResult
	{
		MemoryCache,
		DiskCache,
		Disk,
		Internet,
		ApplicationBundle,
		CompiledResource,
	}
}

