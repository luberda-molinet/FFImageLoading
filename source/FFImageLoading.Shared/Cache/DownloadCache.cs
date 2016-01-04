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
		private const int BufferSize = 4096; // Xamarin large object heap threshold is 8K

        public DownloadCache(HttpClient httpClient, IDiskCache diskCache)
        {
			DownloadHttpClient = httpClient;
            _md5Helper = new MD5Helper();
            _diskCache = diskCache;
        }

		public HttpClient DownloadHttpClient { get; set; }

		public async Task<DownloadedData> GetAsync(string url, CancellationToken token, TimeSpan? duration = null, string key = null)
        {
			string filename = string.IsNullOrWhiteSpace(key) ? _md5Helper.MD5(url) : _md5Helper.MD5(key);
			string basePath = await _diskCache.GetBasePathAsync().ConfigureAwait(false);
			string filepath = basePath == null ? filename : Path.Combine(basePath, filename);
			byte[] data = await _diskCache.TryGetAsync(filename, token).ConfigureAwait(false);
			if (data != null)
				return new DownloadedData(filepath, data) { RetrievedFromDiskCache = true };

			using (var memoryStream = await DownloadAndCacheAsync(url, filename, filepath, token, duration).ConfigureAwait(false))
			{
				return new DownloadedData(filepath, memoryStream == null ? null : memoryStream.ToArray());
			}
        }

		public async Task<CacheStream> GetStreamAsync(string url, CancellationToken token, TimeSpan? duration = null, string key = null)
		{
			string filename = string.IsNullOrWhiteSpace(key) ? _md5Helper.MD5(url) : _md5Helper.MD5(key);
			string basePath = await _diskCache.GetBasePathAsync().ConfigureAwait(false);
			string filepath = basePath == null ? filename : Path.Combine(basePath, filename);
			var diskStream = await _diskCache.TryGetStreamAsync(filename).ConfigureAwait(false);
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

				var responseBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

				var memoryStream = new MemoryStream(responseBytes, false);
				memoryStream.Position = 0;

				_diskCache.AddToSavingQueueIfNotExists(filename, responseBytes, duration.Value);

				return memoryStream;
			}
		}
    }
}

