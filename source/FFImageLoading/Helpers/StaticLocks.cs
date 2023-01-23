using System;
using System.Threading;

namespace FFImageLoading.Helpers
{
	internal static class StaticLocks
	{
		public static SemaphoreSlim DecodingLock { get; set; }
	}
}
