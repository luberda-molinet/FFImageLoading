using System;
using UIKit;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher: IMainThreadDispatcher
    {
        public void Post(Action action)
        {
            UIApplication.SharedApplication.BeginInvokeOnMainThread(action);
        }

        public Task PostAsync(Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            Post(() => {
                try {
                    action();
                    tcs.SetResult(string.Empty);
                } catch (Exception ex) {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}
