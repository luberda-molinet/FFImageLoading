using System;
using Android.Graphics.Drawables;
using FFImageLoading.Work;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Cache;

namespace FFImageLoading
{
	public class UrlStreamResolver : IStreamResolver
	{

		protected TaskParameter Parameters { get; private set; }
		protected IDownloadCache DownloadCache { get; private set; }

		public UrlStreamResolver(TaskParameter parameter, IDownloadCache downloadCache) {
			Parameters = parameter;
			DownloadCache = downloadCache;
		}
		
		public async Task<WithLoadingResult<Stream>> GetStream(string identifier)
		{
			var cachedStream = await DownloadCache.GetStreamAsync(identifier, Parameters.CacheDuration).ConfigureAwait(false);
			return WithLoadingResult.Encapsulate(cachedStream.ImageStream,
				cachedStream.RetrievedFromDiskCache ? LoadingResult.DiskCache : LoadingResult.Disk);
		}

		public void Dispose() {
			Parameters = null;
			DownloadCache = null;
		}
		
	}
}

