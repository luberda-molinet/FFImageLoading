using System;
using System.Xml.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal class SKSvgMask
    {
        public SKSvgMask(SKPaint fill, XElement element)
        {
            Fill = fill.Clone();
            Element = element;
        }

        public SKPaint Fill { get; }

        public XElement Element { get; }
    }
}
