using System;
using UIKit;

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
            MainThreadDispatcher.Instance.Post(() =>
            {
               Scale = UIScreen.MainScreen.Scale;
            });
        }
    }
}

