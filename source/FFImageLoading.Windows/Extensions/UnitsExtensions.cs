using FFImageLoading.Helpers;
using System;
using Windows.Graphics.Display;

namespace FFImageLoading.Extensions
{
    public static class UnitsExtensions
    {
        static UnitsExtensions()
        {
            InitResolutionScale();
        }

        static double resolutionScale = -1d;

        static async void InitResolutionScale()
        {
            await MainThreadDispatcher.Instance.PostAsync(() =>
            {
                resolutionScale = (double)DisplayInformation.GetForCurrentView().ResolutionScale / 100.0d;
            }).ConfigureAwait(false);
        }

        static void WaitForResolutionScaleInit()
        {
            while (resolutionScale == -1d) { /* wait */ }
        }

        public static int PointsToPixels(this double points)
        {
            WaitForResolutionScaleInit();

            return (int)Math.Floor(points * resolutionScale);
        }

        public static int PixelsToPoints(this double px)
        {
            if (px == 0d)
                return 0;

            WaitForResolutionScaleInit();

            return (int)Math.Floor(px / resolutionScale);
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
