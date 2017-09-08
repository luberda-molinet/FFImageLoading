using System;

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
#if __IOS__
                Scale = UIKit.UIScreen.MainScreen.Scale;
#elif __MACOS__
                Scale = AppKit.NSScreen.MainScreen.BackingScaleFactor;
#endif
            });
        }
    }
}

