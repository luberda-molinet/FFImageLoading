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
                async () => await DownloadAsync(url, token, configuration.HttpClient, parameters.OnDownloadProgress).ConfigureAwait(false),
                DelayBetweenRetry,
                parameters.RetryCount,
                () => configuration.Logger.Debug(string.Format("Retry download: {0}", url))).ConfigureAwait(false);

            if (responseBytes == null)
                throw new HttpRequestException("No Content");

            if (allowDiskCaching)
            {
                Action finishedAction = null;

                if (parameters.OnFileWriteFinished != null)
                {
                    finishedAction = new Action(() =>
                    {
                        parameters.OnFileWriteFinished(new FileWriteInfo(filePath, url));
                    });
                }


                await configuration.DiskCache.AddToSavingQueueIfNotExistsAsync(filename, responseBytes, duration, finishedAction).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            filePath = await configuration.DiskCache.GetFilePathAsync(filename).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var memoryStream = new MemoryStream(responseBytes, false);
            return new CacheStream(memoryStream, false, filePath);
        }

        protected virtual async Task<byte[]> DownloadAsync(string url, CancellationToken token, HttpClient client, Action<DownloadProgress> progressAction)
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

                                int total = (int)((progressAction != null && response.Content.Headers.ContentLength.HasValue) ? response.Content.Headers.ContentLength.Value : -1);
                                var canReportProgress = total != -1 && progressAction != null;

                                try
                                {
                                    return await Task.Run(async () =>
                                    {
                                        if (canReportProgress)
                                        {
                                            using (var outputStream = new MemoryStream())
                                            using (var sourceStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                                            {
                                                int totalRead = 0;
                                                var buffer = new byte[4096];

                                                int read = 0;
                                                while ((read = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                    outputStream.Write(buffer, 0, read);
                                                    totalRead += read;
                                                    progressAction(new DownloadProgress() { Total = total, Current = totalRead });
                                                }

                                                return outputStream.ToArray();
                                            }
                                        }

                                        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                                    }, cancelReadTimeoutToken.Token).ConfigureAwait(false);
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

