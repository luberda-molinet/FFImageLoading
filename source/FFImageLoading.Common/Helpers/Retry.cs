using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FFImageLoading
{
    [Preserve(AllMembers = true)]
    public static class Retry
    {
        public static async Task<T> DoAsync<T>(Func<Task<T>> action, TimeSpan retryInterval, int retryCount, Action onRetry = null)
        {
            var exceptions = new List<Exception>();

            for (int retry = -1; retry < retryCount; retry++)
            {
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        throw;

                    onRetry?.Invoke();
                    exceptions.Add(ex);
                }

                await Task.Delay(retryInterval).ConfigureAwait(false);
            }

            throw new DownloadAggregateException(exceptions);
        }
    }
}
