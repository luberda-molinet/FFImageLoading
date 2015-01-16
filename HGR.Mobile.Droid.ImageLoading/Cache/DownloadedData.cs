using System;

namespace HGR.Mobile.Droid.ImageLoading.Cache
{
    public class DownloadedData
    {
        public DownloadedData(string cachedPath, byte[] bytes)
        {
            CachedPath = cachedPath;
            Bytes = bytes;
        }

        public string CachedPath { get; private set; }
        public byte[] Bytes { get; private set; }
    }
}

