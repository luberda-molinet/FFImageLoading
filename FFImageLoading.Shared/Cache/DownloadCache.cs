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
        private readonly MD5Helper _md5Helper;
        private readonly IDiskCache _diskCache;

        public DownloadCache(HttpClient httpClient, IDiskCache diskCache)
        {
			DownloadHttpClient = httpClient;
            _md5Helper = new MD5Helper();
            _diskCache = diskCache;
        }

		public HttpClient DownloadHttpClient { get; set; }

		public async Task<DownloadedData> GetAsync(string url, TimeSpan? duration = null)
        {
            string filename = _md5Helper.MD5(url);
            string filepath = Path.Combine(_diskCache.BasePath, filename);
            byte[] data = await _diskCache.TryGetAsync(filename).ConfigureAwait(false);
            if (data != null)
                return new DownloadedData(filepath, data);

			data = await DownloadAndCacheAsync(url, filename, filepath, duration).ConfigureAwait(false);
            return new DownloadedData(filepath, data);
        }

		public async Task<CacheStream> GetStreamAsync(string url, TimeSpan? duration = null)
		{
			string filename = _md5Helper.MD5(url);
			string filepath = Path.Combine(_diskCache.BasePath, filename);
			var stream = _diskCache.TryGetStream(filename);
			if (stream != null)
				return new CacheStream(stream, true);

			var data = await DownloadAndCacheAsync(url, filename, filepath, duration).ConfigureAwait(false);
			return new CacheStream(new MemoryStream(data), false);
		}

		private async Task<byte[]> DownloadAndCacheAsync(string url, string filename, string filepath, TimeSpan? duration)
		{
			if (duration == null)
				duration = new TimeSpan(30, 0, 0, 0); // by default we cache data 30 days
			
			var data = await DownloadHttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
			if (data == null)
				data = new byte[0];

			if (data.Length == 0)
			{
				// this is a strange situation so let's not cache this too long: here 5 minutes
				duration = new TimeSpan(0, 5, 0);
			}

			// this ensures the fullpath exists
			await _diskCache.AddOrUpdateAsync(filename, data, duration.Value).ConfigureAwait(false);
			return data;
		}
    }
}

