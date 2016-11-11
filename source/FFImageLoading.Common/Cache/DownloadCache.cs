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
        public DownloadCache(Configuration configuration)
        {
            Configuration = configuration;
        }

        const int BufferSize = 4096;

        protected Configuration Configuration { get; private set; }

        protected virtual IMD5Helper MD5Helper { get { return Configuration.MD5Helper; } }

        public virtual TimeSpan DelayBetweenRetry { get; set; } = TimeSpan.FromSeconds(1);

        public virtual async Task<CacheStream> DownloadAndCacheIfNeededAsync(string url, TaskParameter parameters, Configuration configuration, CancellationToken token)
        {
            var allowCustomKey = !string.IsNullOrWhiteSpace(parameters.CustomCacheKey)
                                       && (string.IsNullOrWhiteSpace(parameters.LoadingPlaceholderPath) || parameters.LoadingPlaceholderPath != url)
                                       && (string.IsNullOrWhiteSpace(parameters.ErrorPlaceholderPath) || parameters.ErrorPlaceholderPath != url);

            string filename = (allowCustomKey ? MD5Helper.MD5(parameters.CustomCacheKey) : MD5Helper.MD5(url))?.ToSanitizedKey();
            var allowDiskCaching = AllowDiskCaching(parameters.CacheType);
            var duration = parameters.CacheDuration.HasValue ? parameters.CacheDuration.Value : configuration.DiskCacheDuration;
            string filePath = null;

            if (allowDiskCaching)
            {
                var diskStream = await configuration.DiskCache.TryGetStreamAsync(filename).ConfigureAwait(false);
                if (diskStream != null)
                {
                    token.ThrowIfCancellationRequested();
                    filePath = await configuration.DiskCache.GetFilePathAsync(filename).ConfigureAwait(false);
                    return new CacheStream(diskStream, true, filePath);
                }
            }

            token.ThrowIfCancellationRequested();

            var downloadInfo = new DownloadInformation(url, parameters.CustomCacheKey, filename, allowDiskCaching, duration);
            parameters.OnDownloadStarted?.Invoke(downloadInfo);

            var responseBytes = await Retry.DoAsync(
                async () => await DownloadAsync(url, token, configuration.HttpClient).ConfigureAwait(false),
                DelayBetweenRetry,
                parameters.RetryCount,
                () => configuration.Logger.Debug(string.Format("Retry download: {0}", url)));

            if (responseBytes == null)
                throw new HttpRequestException("No Content");

            if (allowDiskCaching)
            {
                await configuration.DiskCache.AddToSavingQueueIfNotExistsAsync(filename, responseBytes, duration).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            filePath = await configuration.DiskCache.GetFilePathAsync(filename).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var memoryStream = new MemoryStream(responseBytes, false);
            return new CacheStream(memoryStream, false, filePath);
        }

        protected virtual async Task<byte[]> DownloadAsync(string url, CancellationToken token, HttpClient client)
        {
            using (var cancelHeadersToken = new CancellationTokenSource())
            {
                cancelHeadersToken.CancelAfter(TimeSpan.FromSeconds(Configuration.HttpHeadersTimeout));

                using (var linkedHeadersToken = CancellationTokenSource.CreateLinkedTokenSource(token, cancelHeadersToken.Token))
                {
                    try
                    {
                        using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linkedHeadersToken.Token).ConfigureAwait(false))
                        {
                            if (!response.IsSuccessStatusCode)
                                throw new HttpRequestException(response.StatusCode.ToString());

                            if (response.Content == null)
                                throw new HttpRequestException("No Content");

                            using (var cancelReadTimeoutToken = new CancellationTokenSource())
                            {
                                cancelReadTimeoutToken.CancelAfter(TimeSpan.FromSeconds(Configuration.HttpReadTimeout));

                                try
                                {
                                    return await Task.Run(async () => await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false), 
                                                          cancelReadTimeoutToken.Token).ConfigureAwait(false);
                                }
                                catch (OperationCanceledException)
                                {
                                    if (cancelReadTimeoutToken.IsCancellationRequested)
                                        throw new Exception("HttpReadTimeout");
                                    else
                                        throw;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (cancelHeadersToken.IsCancellationRequested)
                            throw new Exception("HttpHeadersTimeout");
                        else
                            throw;
                    }
                }
            }
        }

        protected virtual bool AllowDiskCaching(CacheType? cacheType)
        {
            return cacheType.HasValue == false || cacheType == CacheType.All || cacheType == CacheType.Disk;
        }
    }
}

