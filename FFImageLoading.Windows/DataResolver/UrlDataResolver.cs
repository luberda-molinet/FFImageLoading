using FFImageLoading.Cache;
using FFImageLoading.Extensions;
using FFImageLoading.Work;
using System.Threading;
using System.Threading.Tasks;

namespace FFImageLoading.DataResolver
{
    public class UrlDataResolver : IDataResolver
    {

        protected TaskParameter Parameters { get; private set; }
        protected IDownloadCache DownloadCache { get; private set; }

        public UrlDataResolver(TaskParameter parameter, IDownloadCache downloadCache)
        {
            Parameters = parameter;
            DownloadCache = downloadCache;
        }

        public async Task<ResolverImageData> GetData(string identifier, CancellationToken token)
        {
            var downloadedData = await DownloadCache.GetAsync(identifier, token, Parameters.CacheDuration).ConfigureAwait(false);
            var bytes = downloadedData.Bytes;
            var path = downloadedData.CachedPath;
            var result = downloadedData.RetrievedFromDiskCache ? LoadingResult.DiskCache : LoadingResult.Internet;

            return new ResolverImageData() {
                Data = bytes,
                Result = result,
                ResultIdentifier = path
            };
        }

        public void Dispose()
        {
            Parameters = null;
            DownloadCache = null;
        }
    }
}
