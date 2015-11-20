using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using System.Threading;

namespace FFImageLoading.Cache
{
    public class DownloadCache: IDownloadCache
    {
        private readonly MD5Helper _md5Helper;
        private readonly IDiskCache _diskCache;
		private const int BufferSize = 4096; // default value of .NET framework for CopyToAsync buffer size

        public DownloadCache(HttpClient httpClient, IDiskCache diskCache)
        {
			DownloadHttpClient = httpClient;
            _md5Helper = new MD5Helper();
            _diskCache = diskCache;
        }

		public HttpClient DownloadHttpClient { get; set; }

		public async Task<DownloadedData> GetAsync(string url, CancellationToken token, TimeSpan? duration = null)
        {
            string filename = _md5Helper.MD5(url);
            string filepath = _diskCache.BasePath == null ? filename : Path.Combine(_diskCache.BasePath, filename);
			byte[] data = await _diskCache.TryGetAsync(filename, token).ConfigureAwait(false);
			if (data != null)
				return new DownloadedData(filepath, data) { RetrievedFromDiskCache = true };

			using (var memoryStream = await DownloadAndCacheAsync(url, filename, filepath, token, duration).ConfigureAwait(false))
			{
				return new DownloadedData(filepath, memoryStream == null ? null : memoryStream.ToArray());
			}
        }

		public async Task<CacheStream> GetStreamAsync(string url, CancellationToken token, TimeSpan? duration = null)
		{
			string filename = _md5Helper.MD5(url);
			string filepath = _diskCache.BasePath == null ? filename : Path.Combine(_diskCache.BasePath, filename);
			var diskStream = await _diskCache.TryGetStream(filename);
			if (diskStream != null)
				return new CacheStream(diskStream, true);

			var memoryStream = await DownloadAndCacheAsync(url, filename, filepath, token, duration).ConfigureAwait(false);
			return new CacheStream(memoryStream, false);
		}

		private async Task<MemoryStream> DownloadAndCacheAsync(string url, string filename, string filepath, CancellationToken token, TimeSpan? duration)
		{
			if (duration == null)
				duration = new TimeSpan(30, 0, 0, 0); // by default we cache data 30 days

			int headersTimeout = ImageService.Config.HttpHeadersTimeout;
			int readTimeout = ImageService.Config.HttpReadTimeout - headersTimeout;

			var cancelHeadersToken = new CancellationTokenSource();
			cancelHeadersToken.CancelAfter(TimeSpan.FromSeconds(headersTimeout));
			var linkedHeadersToken = CancellationTokenSource.CreateLinkedTokenSource(token, cancelHeadersToken.Token);

			using (var response = await DownloadHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linkedHeadersToken.Token).ConfigureAwait(false))
			{
				if (!response.IsSuccessStatusCode || response.Content == null)
					return null;

				var cancelReadToken = new CancellationTokenSource();
				cancelReadToken.CancelAfter(TimeSpan.FromSeconds(readTimeout));
				var linkedReadToken = CancellationTokenSource.CreateLinkedTokenSource(token, cancelHeadersToken.Token);

				using (var httpStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
				{
					int defaultCapacity = BufferSize;
					if (response.Content.Headers != null && response.Content.Headers.ContentLength != null)
					{
						defaultCapacity = (int)response.Content.Headers.ContentLength.Value;
					}

					var memoryStream = new MemoryStream(defaultCapacity);
					await httpStream.CopyToAsync(memoryStream, BufferSize, linkedReadToken.Token).ConfigureAwait(false);

					if (memoryStream.Length == 0)
					{
						// this is a strange situation so let's not cache this too long: here 5 minutes
						duration = new TimeSpan(0, 5, 0);
					}

					memoryStream.Position = 0; // return to the beginning of the MemoryStream
					// this ensures the fullpath exists
					await _diskCache.AddOrUpdateAsync(filename, memoryStream, token, duration.Value).ConfigureAwait(false);

					memoryStream.Position = 0; // return to the beginning of the MemoryStream
					return memoryStream;
				}
			}
		}
    }
}

