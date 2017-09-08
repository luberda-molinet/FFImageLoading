using System;
using AppKit;

namespace FFImageLoading.Helpers
{
    public static class ScaleHelper
    {
        public static nfloat Scale
        {
            get;
            private set;
        }

        public static void Init()
        {
            if (Scale > 0)
                return;

            MainThreadDispatcher.Instance.Post(() =>
            {
                Scale = NSScreen.MainScreen.BackingScaleFactor;//UIScreen.MainScreen.Scale;
            });
        }
    }
}

