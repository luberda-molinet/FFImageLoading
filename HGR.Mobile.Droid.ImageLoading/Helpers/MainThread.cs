using System;
using Android.OS;

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
    }
}

