using System;
using System.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal struct SKLine
    {
        public SKPoint P1 { get; }
        public SKPoint P2 { get; }

        public SKLine(SKPoint p1, SKPoint p2)
        {
            P1 = p1;
            P2 = p2;
        }
    }
}
