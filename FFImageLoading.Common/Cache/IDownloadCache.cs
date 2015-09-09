using System;
using System.Threading.Tasks;
using System.IO;

namespace FFImageLoading.Cache
{
    public interface IDownloadCache
    {
        Task<DownloadedData> GetAsync(string url, TimeSpan? duration = null);

		Task<CacheStream> GetStreamAsync(string url, TimeSpan? duration = null);
    }
}

