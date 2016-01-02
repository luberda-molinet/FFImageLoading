using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FFImageLoading.Extensions
{
    public static class UnitsExtensions
    {
        public static int PointsToPixels(this double points)
        {
            var scale = (double)Application.Current.Host.Content.ScaleFactor / 100.0f;
            return (int)Math.Floor(points * scale);
        }

        public static int PixelsToPoints(this double px)
        {
            var scale = (double)Application.Current.Host.Content.ScaleFactor / 100.0f;
            return (int)Math.Floor(px / scale);
        }
    }
}
