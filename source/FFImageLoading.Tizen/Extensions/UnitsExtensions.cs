namespace FFImageLoading.Extensions
{
    public static class UnitsExtensions
    {
        public static int DpToPixels(this double dp)
        {
            return ImageService.DpToPixels(dp);
        }

        public static double PixelsToDp(this int px)
        {
            return ImageService.PixelToDP(px);
        }
    }
}

