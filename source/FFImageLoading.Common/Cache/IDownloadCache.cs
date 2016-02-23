using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace FFImageLoading.Cache
{
    public interface IDownloadCache
    {
		Task<string> GetDiskCacheFilePathAsync(string url, string key = null);

		HttpClient DownloadHttpClient { get; set; }

		Task<DownloadedData> GetAsync(string url, CancellationToken token, TimeSpan? duration = null, string key = null);

		Task<CacheStream> GetStreamAsync(string url, CancellationToken token, TimeSpan? duration = null, string key = null);
    }
}

