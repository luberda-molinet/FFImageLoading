using System;

namespace FFImageLoading.Helpers
{
    public static class ScaleHelper
    {
        static nfloat? _scale;
        public static nfloat Scale
        {
            get
            {
                if (!_scale.HasValue)
                {
                    Init();
                }

                return _scale.Value;
            }
        }

        public static void Init()
        {
            if (_scale.HasValue)
                return;

            new MainThreadDispatcher().PostAsync(() =>
            {
#if __IOS__
                _scale = UIKit.UIScreen.MainScreen.Scale;
#elif __MACOS__
                _scale = AppKit.NSScreen.MainScreen.BackingScaleFactor;
#endif
            }).ConfigureAwait(false).GetAwaiter().GetResult();;
        }
    }
}

