using System;
using FFImageLoading.Work;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using System.Threading;
using FFImageLoading.Config;

namespace FFImageLoading.DataResolvers
{
    public class UrlDataResolver : IDataResolver
    {
        public UrlDataResolver(Configuration configuration)
        {
            Configuration = configuration;
        }

        protected IDownloadCache DownloadCache { get { return Configuration.DownloadCache; } }
        protected Configuration Configuration { get; private set; }

        public async Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var downloadedData = await DownloadCache.DownloadAndCacheIfNeededAsync(identifier, parameters, Configuration, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
            {
                downloadedData?.ImageStream?.Dispose();
                token.ThrowIfCancellationRequested();
            }

            var imageInformation = new ImageInformation();
            imageInformation.SetPath(identifier);
            imageInformation.SetFilePath(downloadedData?.FilePath);

            return new Tuple<Stream, LoadingResult, ImageInformation>(
                downloadedData?.ImageStream, downloadedData.RetrievedFromDiskCache ? LoadingResult.DiskCache : LoadingResult.Internet, imageInformation);
        }
    }
}

