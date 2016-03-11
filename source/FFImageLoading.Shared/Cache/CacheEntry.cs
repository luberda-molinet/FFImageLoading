using System;

namespace FFImageLoading.Cache
{
	struct CacheEntry
	{
		public DateTime Origin;
		public TimeSpan TimeToLive;
		public string FileName;

		public CacheEntry (DateTime o, TimeSpan ttl, string fileName)
		{
			Origin = o;
			TimeToLive = ttl;
			FileName = fileName;
		}
	}
}

