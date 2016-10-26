using System;
using UIKit;
using System.Threading.Tasks;
using Foundation;
using CoreFoundation;

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

        public void Post(Action action)
        {
			if (NSThread.Current.IsMainThread)
			{
				action();
			}
			else	
			{
				DispatchQueue.MainQueue.DispatchAsync(action);
			}
        }

        public Task PostAsync(Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            Post(() => {
                try {
                    if (action != null)
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
