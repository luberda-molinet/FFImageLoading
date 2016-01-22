using System;
using UIKit;
using CoreFoundation;
using FFImageLoading.Helpers;

namespace FFImageLoading.Extensions
{
	public static class UnitsExtensions
	{
		public static int PointsToPixels(this double points)
		{
			return (int)Math.Floor(points * ScaleHelper.Scale);
		}

		public static int PixelsToPoints(this double px)
		{
			if (px == 0d)
				return 0;

			return (int)Math.Floor(px / ScaleHelper.Scale);
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

