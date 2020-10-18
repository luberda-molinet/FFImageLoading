using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher : IMainThreadDispatcher
    {
        private CoreDispatcher _dispatcher;

        public async void Post(Action action)
        {
            if (action == null)
                return;

            if(_dispatcher == null)
            {
                _dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            }

            // already in UI thread:
            if (_dispatcher.HasThreadAccess)
            {
                action();
            }
            // not in UI thread, ensuring UI thread:
            else
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
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
