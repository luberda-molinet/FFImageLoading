using System;
namespace FFImageLoading
{
    public struct DownloadInformation
    {
        public DownloadInformation(string url, string customCacheKey, string fileName, bool allowDiskCaching, TimeSpan cacheValidity)
        {
            Url = url;
            CustomCacheKey = customCacheKey;
            FileName = fileName;
            AllowDiskCaching = allowDiskCaching;
            CacheValidity = cacheValidity;
        }

        public string Url { get; internal set; }

        public string CustomCacheKey { get; internal set; }

        public string FileName { get; internal set; }

        public bool AllowDiskCaching { get; internal set; }

        public TimeSpan CacheValidity { get; internal set; }
    }
}
