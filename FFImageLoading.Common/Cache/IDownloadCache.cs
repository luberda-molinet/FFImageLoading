using System;
using System.Threading.Tasks;

namespace FFImageLoading.Cache
{
    public interface IDownloadCache
    {
        Task<DownloadedData> GetAsync(string url, TimeSpan? duration = null);
    }
}

