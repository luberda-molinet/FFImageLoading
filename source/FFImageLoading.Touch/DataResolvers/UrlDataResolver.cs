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
		public UrlDataResolver(TaskParameter parameters, Configuration configuration) 
        {
			Parameters = parameters;
            Configuration = configuration;
		}

        protected TaskParameter Parameters { get; private set; }
        protected IDownloadCache DownloadCache { get { return Configuration.DownloadCache; } }
        protected Configuration Configuration { get; private set; }
		
        public async Task<Tuple<Stream, LoadingResult, ImageInformation, DownloadInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
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

            return new Tuple<Stream, LoadingResult, ImageInformation, DownloadInformation>(
                downloadedData?.ImageStream, downloadedData.RetrievedFromDiskCache ? LoadingResult.DiskCache : LoadingResult.Internet, imageInformation, null);
        }
    }
}

