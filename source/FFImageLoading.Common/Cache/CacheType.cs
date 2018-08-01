using System;

namespace FFImageLoading.Cache
{
    [Preserve(AllMembers = true)]
    public enum CacheType
    {
        Memory,
        Disk,
        All,
        None
    }
}

