using System;
using Foundation;
using UIKit;

namespace FFImageLoading.Helpers
{
	public static class ScaleHelper
	{
		private static readonly Lazy<nfloat> _scale = new Lazy<nfloat>(() =>
		{
            nfloat scale = 1;
            MainThreadDispatcher.Instance.Post(() =>
            {
                scale = UIScreen.MainScreen.Scale;
            });
            return scale;
		});

		public static nfloat Scale
		{
			get
			{
				return _scale.Value;
			}
		}
	}
}

