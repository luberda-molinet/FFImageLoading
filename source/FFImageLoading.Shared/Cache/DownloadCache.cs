using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using FFImageLoading.Helpers;
using System.Threading;
using FFImageLoading.Work;
using FFImageLoading.Config;

namespace FFImageLoading.Cache
{
    public class DownloadCache : IDownloadCache
    {
        const int BufferSize = 4096;

        public virtual MD5Helper MD5Helper { get; private set; } = new MD5Helper();

        public virtual TimeSpan DelayBetweenRetry { get; set; } = TimeSpan.FromSeconds(1);

        public virtual async Task<CacheStream> DownloadAndCacheIfNeededAsync(string url, TaskParameter parameters, Configuration configuration, CancellationToken token)
        {
            var allowCustomKey = string.IsNullOrWhiteSpace(parameters.CustomCacheKey) 
                                       && (string.IsNullOrWhiteSpace(parameters.LoadingPlaceholderPath) || parameters.LoadingPlaceholderPath != url)
                                       && (string.IsNullOrWhiteSpace(parameters.ErrorPlaceholderPath) || parameters.ErrorPlaceholderPath != url);

            string filename = (allowCustomKey ? MD5Helper.MD5(url) : MD5Helper.MD5(parameters.CustomCacheKey))?.ToSanitizedKey();
            var allowDiskCaching = AllowDiskCaching(parameters.CacheType);
            var duration = parameters.CacheDuration.HasValue ? parameters.CacheDuration.Value : configuration.DiskCacheDuration;
            string filePath = null;

            if (allowDiskCaching)
            {
                var diskStream = await configuration.DiskCache.TryGetStreamAsync(filename).ConfigureAwait(false);
                if (diskStream != null)
                {
                    filePath = await configuration.DiskCache.GetFilePathAsync(filename).ConfigureAwait(false);
                    return new CacheStream(diskStream, true, filePath);
                }
            }

            var downloadInfo = new DownloadInformation(url, parameters.CustomCacheKey, filename, allowDiskCaching, duration);
            parameters.OnDownloadStarted?.Invoke(downloadInfo);

            var responseBytes = await Retry.DoAsync(
                async () => await DownloadAsync(url, token, configuration.HttpClient).ConfigureAwait(false),
                DelayBetweenRetry,
                parameters.RetryCount,
                () => configuration.Logger.Debug(string.Format("Retry download: {0}", url)));

            if (responseBytes == null)
                return null;

            var memoryStream = new MemoryStream(responseBytes, false);
            memoryStream.Position = 0;

            if (allowDiskCaching)
            {
                await configuration.DiskCache.AddToSavingQueueIfNotExistsAsync(filename, responseBytes, duration).ConfigureAwait(false);
            }

            filePath = await configuration.DiskCache.GetFilePathAsync(filename).ConfigureAwait(false);
            return new CacheStream(memoryStream, false, filePath);
        }

        async Task<byte[]> DownloadAsync(string url, CancellationToken token, HttpClient client)
        {
            using (var cancelHeadersToken = new CancellationTokenSource())
            {
                cancelHeadersToken.CancelAfter(TimeSpan.FromSeconds(ImageService.Instance.Config.HttpHeadersTimeout));

                using (var linkedHeadersToken = CancellationTokenSource.CreateLinkedTokenSource(token, cancelHeadersToken.Token))
                {
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linkedHeadersToken.Token).ConfigureAwait(false))
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

        static bool AllowDiskCaching(CacheType? cacheType)
        {
            return cacheType.HasValue == false || cacheType == CacheType.All || cacheType == CacheType.Disk;
        }
    }
}

