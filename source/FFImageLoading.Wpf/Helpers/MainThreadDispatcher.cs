using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher : IMainThreadDispatcher
    {
        private Dispatcher _dispatcher;

        public async void Post(Action action)
        {
            if (action == null)
                return;

            if(_dispatcher == null)
            {
                _dispatcher = System.Windows.Application.Current.Dispatcher;
            }

            // already in UI thread:
            if (_dispatcher.CheckAccess())
            {
                action();
            }
            // not in UI thread, ensuring UI thread:
            else
            {
                //var tcs = new TaskCompletionSource<bool>();
                //Post(() =>
                //{
                //    try
                //    {
                //        action?.Invoke();
                //        tcs.SetResult(true);
                //    }
                //    catch (Exception ex)
                //    {
                //        tcs.SetException(ex);
                //    }
                //});

                //await tcs.Task;
                await _dispatcher.InvokeAsync(() => action());
                //await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => action());
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
                    await action?.Invoke();
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
