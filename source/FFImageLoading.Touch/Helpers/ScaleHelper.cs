using System;
using Foundation;
using UIKit;

namespace FFImageLoading.Helpers
{
	public static class ScaleHelper
	{
		private static readonly Lazy<nfloat> _scale = new Lazy<nfloat>(() =>
			{
				if (NSThread.Current.IsMainThread)
				{
					return UIScreen.MainScreen.Scale;
				}
				else	
				{
					nfloat scale = 1;
					UIApplication.SharedApplication.InvokeOnMainThread(() => scale = UIScreen.MainScreen.Scale);
					return scale;
				}
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

