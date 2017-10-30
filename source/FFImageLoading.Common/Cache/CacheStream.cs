using System;
using System.IO;

namespace FFImageLoading.Cache
{
    [Preserve(AllMembers = true)]
	public class CacheStream
	{
        public CacheStream(Stream stream, bool retrievedFromDiskCache, string filePath)
		{
			ImageStream = stream;
			RetrievedFromDiskCache = retrievedFromDiskCache;
            FilePath = filePath;
		}

		public Stream ImageStream { get; private set; }
		public bool RetrievedFromDiskCache { get; private set; }
        public string FilePath { get; private set; }
	}
}

