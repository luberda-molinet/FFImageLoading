using System;
using Windows.Graphics.Display;

namespace FFImageLoading.Extensions
{
    public static class UnitsExtensions
    {
        public static int PointsToPixels(this double points)
        {
            var scale = (double)DisplayInformation.GetForCurrentView().ResolutionScale / 100.0f; 
            return (int)Math.Floor(points * scale);
        }

        public static int PixelsToPoints(this double px)
        {
            var scale = (double)DisplayInformation.GetForCurrentView().ResolutionScale / 100.0f;
            return (int)Math.Floor(px / scale);
        }
    }
}
