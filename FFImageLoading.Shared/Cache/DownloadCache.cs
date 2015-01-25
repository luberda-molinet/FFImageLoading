using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;

namespace FFImageLoading.Cache
{
    public class DownloadCache: IDownloadCache
    {
        private readonly HttpClient _httpClient;
        private readonly MD5Helper _md5Helper;
        private readonly IDiskCache _diskCache;

        public DownloadCache(HttpClient httpClient, IDiskCache diskCache)
        {
            _httpClient = httpClient;
            _md5Helper = new MD5Helper();
            _diskCache = diskCache;
        }

        public async Task<DownloadedData> GetAsync(string url, TimeSpan? duration = null)
        {
            if (duration == null)
                duration = new TimeSpan(30, 0, 0, 0); // by default we cache data 30 days

            string filename = _md5Helper.MD5(url);
            string filepath = Path.Combine(_diskCache.BasePath, filename);
            byte[] data = await _diskCache.TryGetAsync(filename).ConfigureAwait(false);
            if (data != null)
                return new DownloadedData(filepath, data);

            data = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            if (data == null)
                data = new byte[0];

            if (data.Length == 0)
            {
                // this is a strange situation so let's not cache this too long: here 5 minutes
                duration = new TimeSpan(0, 5, 0);
            }

            // this ensures the fullpath exists
            await _diskCache.AddOrUpdateAsync(filename, data, duration.Value).ConfigureAwait(false);
            return new DownloadedData(filepath, data);
        }
    }
}

