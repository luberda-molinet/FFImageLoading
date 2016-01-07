using System;
using UIKit;

namespace FFImageLoading.Extensions
{
	public static class UnitsExtensions
	{
		private static nfloat _screenScale;

		static UnitsExtensions()
		{
			UIScreen.MainScreen.InvokeOnMainThread(() =>
				{
					_screenScale = UIScreen.MainScreen.Scale;
				});
		}

		public static int PointsToPixels(this double points)
		{
			return (int)Math.Floor(points * _screenScale);
		}

		public static int PixelsToPoints(this double px)
		{
			if (px == 0d)
				return 0;

			return (int)Math.Floor(px / UIScreen.MainScreen.Scale);
		}

		public static int PointsToPixels(this int points)
		{
			return PointsToPixels((double)points);
		}

		public static int PixelsToPoints(this int px)
		{
			return PixelsToPoints((double)px);
		}
	}
}

