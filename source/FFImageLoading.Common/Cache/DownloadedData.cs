using System;

namespace FFImageLoading.Cache
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
		public bool RetrievedFromDiskCache { get; set; }
    }
}

