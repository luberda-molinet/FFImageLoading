using System;
using UIKit;

namespace FFImageLoading.Extensions
{
	public static class UnitsExtensions
	{
		public static int PointsToPixels(this double points)
		{
			return (int)Math.Floor(points * UIScreen.MainScreen.Scale);
		}

		public static int PixelsToPoints(this double px)
		{
			return (int)Math.Floor(px / UIScreen.MainScreen.Scale);
		}
	}
}

