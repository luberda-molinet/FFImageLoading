using System;
using System.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal struct SKCircle
    {
        public SKPoint Center { get; }

        public float Radius { get; }

        public SKCircle(SKPoint center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }
}
