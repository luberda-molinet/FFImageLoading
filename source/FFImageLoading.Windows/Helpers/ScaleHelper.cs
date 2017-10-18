using System;
using Windows.Graphics.Display;
using System.Reflection;
using System.Threading;

namespace FFImageLoading.Helpers
{
    public static class ScaleHelper
    {
        static double? _scale;
        public static double Scale
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

            ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
            {
                var displayInfo = DisplayInformation.GetForCurrentView();
                object found = null;

                try
                {
                    found = displayInfo.GetType().GetRuntimeProperty("RawPixelsPerViewPixel")?.GetValue(displayInfo);
                }
                catch (Exception)
                {
                }

                _scale = found == null ? 1d : (double)found;
            }).ConfigureAwait(false).GetAwaiter().GetResult(); ;
        }
    }
}

