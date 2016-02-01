using System;
using FFImageLoading.Work;

namespace FFImageLoading.Extensions
{
	public static class EnumExtensions
	{
		public static bool IsLocalOrCachedResult(this LoadingResult result)
		{
			switch (result)
			{
				case LoadingResult.ApplicationBundle:
				case LoadingResult.CompiledResource:
				case LoadingResult.Disk:
				case LoadingResult.DiskCache:
				case LoadingResult.MemoryCache:
				case LoadingResult.Stream:
					return true;

				default:
					return false;
			}
		}
	}
}

