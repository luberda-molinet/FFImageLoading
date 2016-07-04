using System;
using FFImageLoading.Work;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using System.Threading;

namespace FFImageLoading.Work.DataResolver
{
	public class UrlDataResolver : IDataResolver
	{

		protected TaskParameter Parameters { get; private set; }
		protected IDownloadCache DownloadCache { get; private set; }

		public UrlDataResolver(TaskParameter parameter, IDownloadCache downloadCache) {
			Parameters = parameter;
			DownloadCache = downloadCache;
		}
		
		public async Task<UIImageData> GetData(string identifier, CancellationToken token)
		{
			var downloadedData = await DownloadCache.GetAsync(identifier, token, Parameters.CacheDuration, Parameters.CustomCacheKey, Parameters.CacheType).ConfigureAwait(false);
			var bytes = downloadedData.Bytes;
			var path = downloadedData.CachedPath;
			var result = downloadedData.RetrievedFromDiskCache ? LoadingResult.DiskCache : LoadingResult.Internet;

            var imageInformation = new ImageInformation();
            imageInformation.SetPath(identifier);
            var allowDiskCaching = Parameters.CacheType.HasValue == false || Parameters.CacheType == CacheType.All || Parameters.CacheType == CacheType.Disk;
            if (allowDiskCaching)
            {
                imageInformation.SetFilePath(await DownloadCache.GetDiskCacheFilePathAsync(identifier, Parameters.CustomCacheKey));
            }

			return new UIImageData() { 
				Data = bytes, 
				Result = result, 
				ResultIdentifier = path,
				ImageInformation = imageInformation
			};
		}

		public void Dispose() {
			Parameters = null;
			DownloadCache = null;
		}
		
	}
}

