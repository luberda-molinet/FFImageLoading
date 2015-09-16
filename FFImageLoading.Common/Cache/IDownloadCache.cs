using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;

namespace FFImageLoading.Cache
{
    public interface IDownloadCache
    {
		HttpClient DownloadHttpClient { get; set; }

        Task<DownloadedData> GetAsync(string url, TimeSpan? duration = null);

		Task<CacheStream> GetStreamAsync(string url, TimeSpan? duration = null);
    }
}

