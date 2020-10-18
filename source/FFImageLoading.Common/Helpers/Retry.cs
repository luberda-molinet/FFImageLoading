using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FFImageLoading.Exceptions;

namespace FFImageLoading
{
    [Preserve(AllMembers = true)]
    public static class Retry
    {
        public static async Task<T> DoAsync<T>(Func<Task<T>> action, TimeSpan retryInterval, int retryCount, Action onRetry = null)
        {
            var exceptions = new List<Exception>();

            for (var retry = -1; retry < retryCount; retry++)
            {
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException || ex is DownloadException)
                        throw;

                    if (ex is DownloadHttpStatusCodeException statusCodeException)
                    {
                        switch (statusCodeException.HttpStatusCode)
                        {
                            case System.Net.HttpStatusCode.BadRequest:
                            case System.Net.HttpStatusCode.Conflict:
                            case System.Net.HttpStatusCode.ExpectationFailed:
                            case System.Net.HttpStatusCode.Forbidden:
                            case System.Net.HttpStatusCode.Gone:
                            case System.Net.HttpStatusCode.HttpVersionNotSupported:
                            case System.Net.HttpStatusCode.InternalServerError:
                            case System.Net.HttpStatusCode.MethodNotAllowed:
                            case System.Net.HttpStatusCode.Moved:
                            case System.Net.HttpStatusCode.NoContent:
                            case System.Net.HttpStatusCode.NotFound:
                            case System.Net.HttpStatusCode.PaymentRequired:
                            case System.Net.HttpStatusCode.PreconditionFailed:
                            case System.Net.HttpStatusCode.ProxyAuthenticationRequired:
                            case System.Net.HttpStatusCode.RequestedRangeNotSatisfiable:
                            case System.Net.HttpStatusCode.RequestEntityTooLarge:
                            case System.Net.HttpStatusCode.RequestUriTooLong:
                            case System.Net.HttpStatusCode.Unauthorized:
                            case System.Net.HttpStatusCode.UnsupportedMediaType:
                            case System.Net.HttpStatusCode.UpgradeRequired:
                            case System.Net.HttpStatusCode.UseProxy:
                                throw;

                            default:
                                break;
                        }
                    }

                    onRetry?.Invoke();
                    exceptions.Add(ex);
                }

                await Task.Delay(retryInterval).ConfigureAwait(false);
            }

            throw new DownloadAggregateException(exceptions);
        }
    }
}
