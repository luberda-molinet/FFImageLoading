using System;
using UIKit;
using System.Threading.Tasks;
using Foundation;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher: IMainThreadDispatcher
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

        public void Post(Action action)
        {
			if (NSThread.Current.IsMainThread)
			{
				action();
			}
			else	
			{
				UIApplication.SharedApplication.BeginInvokeOnMainThread(action);	
			}
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
