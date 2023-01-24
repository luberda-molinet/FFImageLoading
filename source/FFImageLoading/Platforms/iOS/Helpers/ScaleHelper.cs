using System;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers
{
    public class ScaleHelper
    {
		public ScaleHelper(IMainThreadDispatcher mainThreadDispatcher)
		{
			MainThreadDispatcher = mainThreadDispatcher;
		}

		protected IMainThreadDispatcher MainThreadDispatcher;

        nfloat? _scale;
        public nfloat Scale
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

        async Task InitAsync()
        {
            if (_scale.HasValue)
                return;

            var dispatcher = MainThreadDispatcher;
            await dispatcher.PostAsync(() =>
            {
#if __IOS__
                _scale = UIKit.UIScreen.MainScreen.Scale;
#elif __MACOS__
                _scale = AppKit.NSScreen.MainScreen.BackingScaleFactor;
#endif
            }).ConfigureAwait(false);
        }
    }
}

