using System;

namespace FFImageLoading.Cache
{
    [Preserve(AllMembers = true)]
	public enum CacheResult
	{
		Found,
		NotFound,
		ErrorOccured
	}
}

