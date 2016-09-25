using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using FFImageLoading.Helpers;
using System.Threading;

namespace FFImageLoading.Cache
{
    public class DownloadCache: IDownloadCache
    {
        private readonly MD5Helper _md5Helper;
        private readonly IDiskCache _diskCache;
        private readonly TimeSpan _diskCacheDuration;
		private const int BufferSize = 4096; // Xamarin large object heap threshold is 8K

        public DownloadCache(HttpClient httpClient, IDiskCache diskCache, TimeSpan diskCacheDuration)
        {
			DownloadHttpClient = httpClient;
            _md5Helper = new MD5Helper();
            _diskCache = diskCache;
            _diskCacheDuration = diskCacheDuration;
        }

		public HttpClient DownloadHttpClient { get; set; }

		public async Task<string> GetDiskCacheFilePathAsync(string url, string key = null)
		{
            string filename = (string.IsNullOrWhiteSpace(key) ? _md5Helper.MD5(url) : _md5Helper.MD5(key))?.ToSanitizedKey();
            return await _diskCache.GetFilePathAsync(filename).ConfigureAwait(false);
		}

		public async Task<DownloadedData> GetAsync(string url, CancellationToken token, Action<DownloadInformation> onDownloadStarted, TimeSpan? duration = null, string key = null, CacheType? cacheType = null)
        {
            string filename = (string.IsNullOrWhiteSpace(key) ? _md5Helper.MD5(url) : _md5Helper.MD5(key))?.ToSanitizedKey();
		    var allowDiskCaching = AllowDiskCaching(cacheType);
            string filepath = allowDiskCaching == false ? null : await _diskCache.GetFilePathAsync(filename).ConfigureAwait(false);

            if (allowDiskCaching)
            {
                byte[] data = await _diskCache.TryGetAsync(filename, token).ConfigureAwait(false);
                if (data != null)
                    return new DownloadedData(filepath, data) { RetrievedFromDiskCache = true };
            }

            var downloadInformation = new DownloadInformation(url, key, filename, allowDiskCaching, duration);
            onDownloadStarted?.Invoke(downloadInformation);

			var bytes = await DownloadBytesAndCacheAsync(url, filename, token, duration, cacheType).ConfigureAwait(false);
			return new DownloadedData(filepath, bytes);
        }

		public async Task<CacheStream> GetStreamAsync(string url, CancellationToken token, Action<DownloadInformation> onDownloadStarted, TimeSpan? duration = null, string key = null, CacheType? cacheType = null)
		{
			string filename = (string.IsNullOrWhiteSpace(key) ? _md5Helper.MD5(url) : _md5Helper.MD5(key))?.ToSanitizedKey();
            var allowDiskCaching = AllowDiskCaching(cacheType);

            if (allowDiskCaching)
            {
                var diskStream = await _diskCache.TryGetStreamAsync(filename).ConfigureAwait(false);
                if (diskStream != null)
                    return new CacheStream(diskStream, true);
            }

            var downloadInformation = new DownloadInformation(url, key, filename, allowDiskCaching, duration);
            onDownloadStarted?.Invoke(downloadInformation);

			var memoryStream = await DownloadStreamAndCacheAsync(url, filename, token, duration, cacheType).ConfigureAwait(false);
			return new CacheStream(memoryStream, false);
		}

		private async Task<MemoryStream> DownloadStreamAndCacheAsync(string url, string filename, CancellationToken token, TimeSpan? duration, CacheType? cacheType)
		{
			var responseBytes = await DownloadAsync(url, filename, token).ConfigureAwait(false);
			if (responseBytes == null)
				return null;

			var memoryStream = new MemoryStream(responseBytes, false);
			memoryStream.Position = 0;

            var allowDiskCaching = AllowDiskCaching(cacheType);
            if (allowDiskCaching)
            {
                await _diskCache.AddToSavingQueueIfNotExistsAsync(filename, responseBytes, duration ?? _diskCacheDuration).ConfigureAwait(false);
            }
			
			return memoryStream;
		}

		private async Task<byte[]> DownloadBytesAndCacheAsync(string url, string filename, CancellationToken token, TimeSpan? duration, CacheType? cacheType)
		{
			var responseBytes = await DownloadAsync(url, filename, token).ConfigureAwait(false);
			if (responseBytes == null)
				return null;

            var allowDiskCaching = AllowDiskCaching(cacheType);
			if (allowDiskCaching)
            {
                await _diskCache.AddToSavingQueueIfNotExistsAsync(filename, responseBytes, duration ?? _diskCacheDuration).ConfigureAwait(false);
            }
			
			return responseBytes;
		}

		private async Task<byte[]> DownloadAsync(string url, string filename, CancellationToken token)
		{
			using (var cancelHeadersToken = new CancellationTokenSource())
			{
				cancelHeadersToken.CancelAfter(TimeSpan.FromSeconds(ImageService.Instance.Config.HttpHeadersTimeout));

				using (var linkedHeadersToken = CancellationTokenSource.CreateLinkedTokenSource(token, cancelHeadersToken.Token))
				{
					using (var response = await DownloadHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linkedHeadersToken.Token).ConfigureAwait(false))
					{
						if (!response.IsSuccessStatusCode || response.Content == null)
							return null;

                        using (var cancelReadTimeoutToken = new CancellationTokenSource())
                        {
                            cancelReadTimeoutToken.CancelAfter(TimeSpan.FromSeconds(ImageService.Instance.Config.HttpReadTimeout));

                            return await Task.Run(async () => await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false), 
                                                  cancelReadTimeoutToken.Token).ConfigureAwait(false);
                        }
					}
				}
			}
		}

        private static bool AllowDiskCaching(CacheType? cacheType)
        {
            return cacheType.HasValue == false || cacheType == CacheType.All || cacheType == CacheType.Disk;
        }
    }
}

