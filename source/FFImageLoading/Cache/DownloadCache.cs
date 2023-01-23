using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using FFImageLoading.Helpers;
using System.Threading;
using FFImageLoading.Work;
using FFImageLoading.Config;
using System.Linq;
using FFImageLoading.Exceptions;

namespace FFImageLoading.Cache
{
    [Preserve(AllMembers = true)]
    public class DownloadCache : IDownloadCache
    {
        public DownloadCache(Configuration configuration)
        {
            Configuration = configuration;
        }

        public string[] InvalidContentTypes { get; set; } = new[] { "text/html", "application/json", "audio/", "video/", "message" };

        protected Configuration Configuration { get; private set; }

        protected virtual IMD5Helper MD5Helper => Configuration.MD5Helper;

        public virtual TimeSpan DelayBetweenRetry { get; set; } = TimeSpan.FromSeconds(1);

        public virtual async Task<CacheStream> DownloadAndCacheIfNeededAsync(string url, TaskParameter parameters, Configuration configuration, CancellationToken token)
        {
            var allowCustomKey = !string.IsNullOrWhiteSpace(parameters.CustomCacheKey)
                                       && (string.IsNullOrWhiteSpace(parameters.LoadingPlaceholderPath) || parameters.LoadingPlaceholderPath != url)
                                       && (string.IsNullOrWhiteSpace(parameters.ErrorPlaceholderPath) || parameters.ErrorPlaceholderPath != url);

            var filename = (allowCustomKey ? MD5Helper.MD5(parameters.CustomCacheKey) : MD5Helper.MD5(url));
            var allowDiskCaching = AllowDiskCaching(parameters.CacheType);
            var duration = parameters.CacheDuration ?? configuration.DiskCacheDuration;
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
                async () => await DownloadAsync(url, token, configuration.HttpClient, parameters, downloadInfo).ConfigureAwait(false),
                parameters.RetryDelayInMs > 0 ? TimeSpan.FromMilliseconds(parameters.RetryDelayInMs) : DelayBetweenRetry,
                parameters.RetryCount,
                () => configuration.Logger.Debug(string.Format("Retry download: {0}", url))).ConfigureAwait(false);

            if (responseBytes == null)
                throw new HttpRequestException("No Content");

            if (allowDiskCaching)
            {
				Action<FileWriteInfo> finishedAction = null;
				var onFileWriteFinished = parameters?.OnFileWriteFinished;
                if (onFileWriteFinished != null)
                {
                    finishedAction = (fileWriteInfo) =>
                    {
                        onFileWriteFinished?.Invoke(fileWriteInfo);
                    };
                }

				await configuration.DiskCache.AddToSavingQueueIfNotExistsAsync(filename, responseBytes, downloadInfo.CacheValidity, url, finishedAction).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            filePath = await configuration.DiskCache.GetFilePathAsync(filename).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var memoryStream = new MemoryStream(responseBytes, false);
            return new CacheStream(memoryStream, false, filePath);
        }

        protected virtual async Task<byte[]> DownloadAsync(string url, CancellationToken token, HttpClient client, TaskParameter parameters, DownloadInformation downloadInformation)
        {
            if (!parameters.Preload)
            {
                await Task.Delay(25).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }

            var progressAction = parameters.OnDownloadProgress;

            using (var httpHeadersTimeoutTokenSource = new CancellationTokenSource())
            using (var headersTimeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, httpHeadersTimeoutTokenSource.Token))
            {
                httpHeadersTimeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(Configuration.HttpHeadersTimeout));

                try
                {
                    var headerTimeoutToken = headersTimeoutTokenSource.Token;

                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, headerTimeoutToken).ConfigureAwait(false))
                    {
                        headerTimeoutToken.ThrowIfCancellationRequested();

                        if (!response.IsSuccessStatusCode)
                        {
                            if (response.Content == null)
                                throw new DownloadHttpStatusCodeException(response.StatusCode);

                            using (response.Content)
                            {
                                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                throw new DownloadHttpStatusCodeException(response.StatusCode, content);
                            }
                        }

                        if (response.Content == null)
                            throw new DownloadException("No content");

                        var mediaType = response.Content.Headers?.ContentType?.MediaType;
                        if (!string.IsNullOrWhiteSpace(mediaType) && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        {
                            if (InvalidContentTypes.Any(v => mediaType.StartsWith(v, StringComparison.OrdinalIgnoreCase)))
                                throw new DownloadException($"Invalid response content type ({mediaType})");
                        }

                        if (!parameters.CacheDuration.HasValue && Configuration.TryToReadDiskCacheDurationFromHttpHeaders
                            && response.Headers?.CacheControl?.MaxAge != null && response.Headers.CacheControl.MaxAge > TimeSpan.Zero)
                        {
                            downloadInformation.CacheValidity = response.Headers.CacheControl.MaxAge.Value;
                        }

                        ModifyParametersAfterResponse(response, parameters);

                        using (var httpReadTimeoutTokenSource = new CancellationTokenSource())
                        using (var readTimeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, httpReadTimeoutTokenSource.Token))
                        {
                            var readTimeoutToken = readTimeoutTokenSource.Token;
                            var httpReadTimeoutToken = httpReadTimeoutTokenSource.Token;
                            var total = (int)(response.Content.Headers.ContentLength ?? -1);
                            var canReportProgress = progressAction != null;

                            httpReadTimeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(Configuration.HttpReadTimeout));
                            readTimeoutToken.ThrowIfCancellationRequested();

                            try
                            {
                                using (var outputStream = new MemoryStream())
                                using (var sourceStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                                {
                                    httpReadTimeoutToken.Register(() => sourceStream.TryDispose());

                                    var totalRead = 0;
                                    var buffer = new byte[Configuration.HttpReadBufferSize];
                                    var read = 0;

                                    while ((read = await sourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                                    {
                                        readTimeoutToken.ThrowIfCancellationRequested();
                                        outputStream.Write(buffer, 0, read);
                                        totalRead += read;

                                        if (canReportProgress)
                                            progressAction(new DownloadProgress(totalRead, total));
                                    }

                                    if (outputStream.Length == 0)
                                        throw new DownloadException("Zero length stream");

                                    if (outputStream.Length < 32)
                                        throw new DownloadException("Invalid stream");

                                    return outputStream.ToArray();
                                }
                            }
                            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
                            {
                                if (httpReadTimeoutTokenSource.IsCancellationRequested)
                                    throw new DownloadReadTimeoutException();

                                throw;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (httpHeadersTimeoutTokenSource.IsCancellationRequested)
                        throw new DownloadHeadersTimeoutException();
                    else
                        throw;
                }
            }
        }

        protected virtual void ModifyParametersAfterResponse(HttpResponseMessage response, TaskParameter parameters)
        {
            // YOUR CUSTOM LOGIC HERE
            // eg: parameters.CacheDuration = response.Headers.CacheControl.MaxAge;
        }

        protected virtual bool AllowDiskCaching(CacheType? cacheType)
        {
            return cacheType.HasValue == false || cacheType == CacheType.All || cacheType == CacheType.Disk;
        }
    }
}

