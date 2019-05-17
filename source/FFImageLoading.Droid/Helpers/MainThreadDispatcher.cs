using System;
using Android.OS;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher : IMainThreadDispatcher
    {
        private static readonly Handler _handler = new Handler(Looper.MainLooper);

        public void Post(Action action)
        {
            var currentLooper = Looper.MyLooper();

            if (currentLooper != null && currentLooper.Thread == Looper.MainLooper.Thread)
            {
                action?.Invoke();
            }
            else
            {
                _handler.Post(action);
            }
        }

        public Task PostAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            Post(() =>
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

            return tcs.Task;
        }

        public Task PostAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<bool>();
            Post(async () =>
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

            return tcs.Task;
        }
    }
}

