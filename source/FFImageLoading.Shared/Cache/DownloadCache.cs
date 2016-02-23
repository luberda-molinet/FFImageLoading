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

		public async Task<string> GetDiskCacheFilePathAsync(string url, string key = null)
		{
			string filename = string.IsNullOrWhiteSpace(key) ? _md5Helper.MD5(url) : _md5Helper.MD5(key);
			return await _diskCache.GetFilePathAsync(filename);
		}

		public async Task<DownloadedData> GetAsync(string url, CancellationToken token, TimeSpan? duration = null, string key = null)
        {
			string filename = string.IsNullOrWhiteSpace(key) ? _md5Helper.MD5(url) : _md5Helper.MD5(key);
			string filepath = await _diskCache.GetFilePathAsync(filename);

			byte[] data = await _diskCache.TryGetAsync(filename, token).ConfigureAwait(false);
			if (data != null)
				return new DownloadedData(filepath, data) { RetrievedFromDiskCache = true };

			var bytes = await DownloadBytesAndCacheAsync(url, filename, filepath, token, duration).ConfigureAwait(false);
			return new DownloadedData(filepath, bytes);
        }

		public async Task<CacheStream> GetStreamAsync(string url, CancellationToken token, TimeSpan? duration = null, string key = null)
		{
			string filename = string.IsNullOrWhiteSpace(key) ? _md5Helper.MD5(url) : _md5Helper.MD5(key);
			string filepath = await _diskCache.GetFilePathAsync(filename);

			var diskStream = await _diskCache.TryGetStreamAsync(filename).ConfigureAwait(false);
			if (diskStream != null)
				return new CacheStream(diskStream, true);

			var memoryStream = await DownloadStreamAndCacheAsync(url, filename, filepath, token, duration).ConfigureAwait(false);
			return new CacheStream(memoryStream, false);
		}

		private async Task<MemoryStream> DownloadStreamAndCacheAsync(string url, string filename, string filepath, CancellationToken token, TimeSpan? duration)
		{
			var responseBytes = await DownloadAsync(url, filename, filepath, token).ConfigureAwait(false);
			if (responseBytes == null)
				return null;

			var memoryStream = new MemoryStream(responseBytes, false);
			memoryStream.Position = 0;

			_diskCache.AddToSavingQueueIfNotExists(filename, responseBytes, duration ?? new TimeSpan(30, 0, 0, 0)); // by default we cache data 30 days)
			return memoryStream;
		}

		private async Task<byte[]> DownloadBytesAndCacheAsync(string url, string filename, string filepath, CancellationToken token, TimeSpan? duration)
		{
			var responseBytes = await DownloadAsync(url, filename, filepath, token).ConfigureAwait(false);
			if (responseBytes == null)
				return null;

			_diskCache.AddToSavingQueueIfNotExists(filename, responseBytes, duration ?? new TimeSpan(30, 0, 0, 0)); // by default we cache data 30 days)
			return responseBytes;
		}

		private async Task<byte[]> DownloadAsync(string url, string filename, string filepath, CancellationToken token)
		{
			int headersTimeout = ImageService.Config.HttpHeadersTimeout;
			// Not used for the moment
			// int readTimeout = ImageService.Config.HttpReadTimeout - headersTimeout;

			using (var cancelHeadersToken = new CancellationTokenSource())
			{
				cancelHeadersToken.CancelAfter(TimeSpan.FromSeconds(headersTimeout));
				using (var linkedHeadersToken = CancellationTokenSource.CreateLinkedTokenSource(token, cancelHeadersToken.Token))
				{
					using (var response = await DownloadHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linkedHeadersToken.Token).ConfigureAwait(false))
					{
						if (!response.IsSuccessStatusCode || response.Content == null)
							return null;

						return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
					}
				}
			}
		}
    }
}

