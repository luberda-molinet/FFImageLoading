using System;
using Android.OS;
using Android.App;
using System.Threading.Tasks;

namespace HGR.Mobile.Droid.ImageLoading.Helpers
{
    public static class MainThread
    {
        public static void Post(Action action)
        {
            // Post on main thread
            Handler handler = new Handler(Looper.MainLooper);
            handler.Post(action);
        }

        public static Task PostAsync(Action action)
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

