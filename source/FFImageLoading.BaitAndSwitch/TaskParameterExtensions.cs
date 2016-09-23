using System;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Work;

namespace FFImageLoading
{
	public static class TaskParameterExtensions
	{
		private const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";

		/// <summary>
		/// Invalidate the image corresponding to given parameters from given caches.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		/// <param name="cacheType">Cache type.</param>
		public static Task InvalidateAsync(this TaskParameter parameters, CacheType cacheType)
		{
			throw new Exception(DoNotReference);
		}

		/// <summary>
		/// Preloads the image request into memory cache/disk cache for future use.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		public static void Preload(this TaskParameter parameters)
		{
			throw new Exception(DoNotReference);
		}

		/// <summary>
		/// Preloads the image request into memory cache/disk cache for future use.
		/// IMPORTANT: It throws image loading exceptions - you should handle them
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		public static Task PreloadAsync(this TaskParameter parameters)
		{
			throw new Exception(DoNotReference);
		}

		/// <summary>
		/// Downloads the image request into disk cache for future use if not already exists.
		/// Only Url Source supported.
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		public static void DownloadOnly(this TaskParameter parameters)
		{
			throw new Exception(DoNotReference);
		}

		/// <summary>
		/// Downloads the image request into disk cache for future use if not already exists.
		/// Only Url Source supported.
		/// IMPORTANT: It throws image loading exceptions - you should handle them
		/// </summary>
		/// <param name="parameters">Image parameters.</param>
		public static async Task DownloadOnlyAsync(this TaskParameter parameters)
		{
			throw new Exception(DoNotReference);
		}
	}
}

