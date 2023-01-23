using System;
using System.Threading.Tasks;
using ElmSharp;

namespace FFImageLoading.Helpers
{
    public class EcoreThreadDispatcher : IMainThreadDispatcher
    {
        public Task PostAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (EcoreMainloop.IsMainThread)
            {
                try
                {
                    action?.Invoke();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
            else
            {
                EcoreMainloop.PostAndWakeUp(() =>
                {
                    try
                    {
                        action?.Invoke();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
            }

            return tcs.Task;
        }

        public async Task PostAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (EcoreMainloop.IsMainThread)
            {
                try
                {
                    await (action?.Invoke()).ConfigureAwait(false);
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
            else
            {
                EcoreMainloop.PostAndWakeUp(async () =>
                {
                    try
                    {
                        await (action?.Invoke()).ConfigureAwait(false);
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
            }

            await tcs.Task.ConfigureAwait(false);
        }
    }
}
