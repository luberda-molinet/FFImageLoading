using System;
using System.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal struct SKRoundedRect
    {
        public SKRoundedRect(SKRect rect, float rx, float ry)
        {
            Rect = rect;
            RadiusX = rx;
            RadiusY = ry;
        }

        public SKRect Rect { get; }

        public float RadiusX { get; }

        public float RadiusY { get; }

        public bool IsRounded => RadiusX > 0 || RadiusY > 0;
    }
}
