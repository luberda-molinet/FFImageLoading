using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.Work;

namespace FFImageLoading.DataResolvers
{
    public class UrlDataResolver : IDataResolver
    {
        public UrlDataResolver(IConfiguration configuration, IDownloadCache downloadCache)
        {
            Configuration = configuration;
			DownloadCache = downloadCache;
        }

        protected IDownloadCache DownloadCache { get; }
        protected IConfiguration Configuration { get; }

        public async virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {

            var downloadedData = await DownloadCache.DownloadAndCacheIfNeededAsync(identifier, parameters, Configuration, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
            {
                downloadedData?.ImageStream.TryDispose();
                token.ThrowIfCancellationRequested();
            }

            var imageInformation = new ImageInformation();
            imageInformation.SetPath(identifier);
            imageInformation.SetFilePath(downloadedData?.FilePath);

            return new DataResolverResult(
                downloadedData?.ImageStream, downloadedData.RetrievedFromDiskCache ? LoadingResult.DiskCache : LoadingResult.Internet, imageInformation);
        }
    }
}
