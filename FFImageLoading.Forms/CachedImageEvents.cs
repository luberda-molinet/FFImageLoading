using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFImageLoading.Forms
{
    public class CachedImageEvents
    {
        public class CacheClearedEventArgs : EventArgs
        {
            public CacheClearedEventArgs(Cache.CacheType cacheType)
            {
                CacheType = cacheType;
            }

            public Cache.CacheType CacheType { get; set; }
        }

        public class CacheInvalidatedEventArgs : EventArgs
        {
            public CacheInvalidatedEventArgs(string key, Cache.CacheType cacheType)
            {
                Key = key;
                CacheType = cacheType;
            }

            public Cache.CacheType CacheType { get; set; }

            public string Key { get; set; }
        }
    }
}
