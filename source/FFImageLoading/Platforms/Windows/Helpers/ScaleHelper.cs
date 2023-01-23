using System;
using Windows.Graphics.Display;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
                    InitAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                }

                return _scale.Value;
            }
        }

        public static async Task InitAsync()
        {
            if (_scale.HasValue)
                return;

            var dispatcher = ImageService.Instance.Config.MainThreadDispatcher;

            await dispatcher.PostAsync(() =>
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
            }).ConfigureAwait(false);
        }
    }
}

