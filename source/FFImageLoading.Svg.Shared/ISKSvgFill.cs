using System;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal interface ISKSvgFill
    {
        void ApplyFill(SKPaint fill, SKRect bounds);
    }
}
