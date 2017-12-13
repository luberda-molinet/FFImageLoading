using System;

namespace FFImageLoading.Extensions
{
    public static class UnitsExtensions
    {
        public static int DpToPixels(this int dp) => ImageService.Instance.DpToPixels(dp);

        public static int DpToPixels(this double dp) => ImageService.Instance.DpToPixels(dp);

        public static double PixelsToDp(this int px) => ImageService.Instance.PixelsToDp(px);

        public static double PixelsToDp(this double px) => ImageService.Instance.PixelsToDp(px);

        [Obsolete("Use DpToPixels")]
        public static int PointsToPixels(this double points) => ImageService.Instance.DpToPixels(points);

        [Obsolete("Use PixelsToDp")]
        public static int PixelsToPoints(this double px) => (int)ImageService.Instance.PixelsToDp(px);

        [Obsolete("Use DpToPixels")]
        public static int PointsToPixels(this int points) => ImageService.Instance.DpToPixels(points);

        [Obsolete("Use PixelsToDp")]
        public static int PixelsToPoints(this int px) => (int)ImageService.Instance.PixelsToDp(px);
    }
}
