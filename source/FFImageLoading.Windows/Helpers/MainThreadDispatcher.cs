using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher : IMainThreadDispatcher
    {
        static MainThreadDispatcher instance;

        public static MainThreadDispatcher Instance
        {
            get
            {
                if (instance == null)
                    instance = new MainThreadDispatcher();

                return instance;
            }
        }

        public async void Post(Action action)
        {
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (action == null)
                return;

			await dispatcher.RunAsync(CoreDispatcherPriority.Low, () => action());
        }

        public Task PostAsync(Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            Post(() => {
                try
                {
                    action();
                    tcs.SetResult(string.Empty);
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
