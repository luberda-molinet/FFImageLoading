using System;
using System.IO;

namespace FFImageLoading.Cache
{
	public class CacheStream
	{
		public CacheStream(Stream stream, bool retrievedFromDiskCache)
		{
			ImageStream = stream;
			RetrievedFromDiskCache = retrievedFromDiskCache;
		}

		public Stream ImageStream { get; private set; }
		public bool RetrievedFromDiskCache { get; private set; }
	}
}

