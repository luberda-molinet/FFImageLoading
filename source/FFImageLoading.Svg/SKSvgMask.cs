using System;
using System.Xml.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal class SKSvgMask
    {
        public SKSvgMask(SKPaint stroke, SKPaint fill, XElement element)
        {
			Stroke = stroke?.Clone();
			Fill = fill?.Clone();
            Element = element;
        }

		public SKPaint Stroke { get; }

		public SKPaint Fill { get; }

        public XElement Element { get; }
    }
}
