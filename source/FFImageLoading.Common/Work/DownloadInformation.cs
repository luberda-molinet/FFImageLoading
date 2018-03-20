using System;
namespace FFImageLoading
{
    public class DownloadInformation
    {
        public DownloadInformation(string url, string customCacheKey, string fileName, bool allowDiskCaching, TimeSpan cacheValidity)
        {
            Url = url;
            CustomCacheKey = customCacheKey;
            FileName = fileName;
            AllowDiskCaching = allowDiskCaching;
            CacheValidity = cacheValidity;
        }

        public string Url { get; private set; }

        public string CustomCacheKey { get; private set; }

        public string FileName { get; private set; }

        public bool AllowDiskCaching { get; private set; }

        public TimeSpan CacheValidity { get; internal set; }
    }
}
