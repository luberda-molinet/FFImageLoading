using System;
using Android.OS;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher: IMainThreadDispatcher
    {
		static Handler _handler = new Handler(Looper.MainLooper);

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
			Looper currentLooper = Looper.MyLooper();

			if(currentLooper != null && currentLooper.Thread == Looper.MainLooper.Thread)
			{
				action();
			}
			else
			{
				_handler.Post(action);	
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

