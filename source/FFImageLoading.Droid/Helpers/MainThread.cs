using System;
using Android.OS;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher: IMainThreadDispatcher
    {
		Handler _handler;

		public MainThreadDispatcher()
		{
			_handler = new Handler(Looper.MainLooper);
		}

        public void Post(Action action)
        {
            _handler.Post(action);
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

