using FFImageLoading.Cache;
using FFImageLoading.Work;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace FFImageLoading.DataResolver
{
    public class UrlDataResolver : IStreamResolver
    {

        protected TaskParameter Parameters { get; private set; }
        protected IDownloadCache DownloadCache { get; private set; }

        public UrlDataResolver(TaskParameter parameter, IDownloadCache downloadCache)
        {
            Parameters = parameter;
            DownloadCache = downloadCache;
        }

        public async Task<WithLoadingResult<Stream>> GetStream(string identifier, CancellationToken token)
        {
            var cachedStream = await DownloadCache.GetStreamAsync(identifier, token, Parameters.CacheDuration, Parameters.CustomCacheKey).ConfigureAwait(false);

            var imageInformation = new ImageInformation();
            imageInformation.SetPath(identifier);
            imageInformation.SetFilePath(await DownloadCache.GetDiskCacheFilePathAsync(identifier, Parameters.CustomCacheKey));

            return WithLoadingResult.Encapsulate(cachedStream.ImageStream,
                cachedStream.RetrievedFromDiskCache ? LoadingResult.DiskCache : LoadingResult.Internet, imageInformation);
        }

        public void Dispose()
        {
            Parameters = null;
            DownloadCache = null;
        }
    }
}
