using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal class SKTextSpan
    {
        public SKTextSpan(string text, SKPaint fill, float? x = null, float? y = null, float? baselineShift = null)
        {
            Text = text;
            Fill = fill;
            X = x;
            Y = y;
            BaselineShift = baselineShift;
        }

        public string Text { get; }

        public SKPaint Fill { get; }

        public float? X { get; }

        public float? Y { get; }

        public float? BaselineShift { get; }

        public float MeasureTextWidth() => Fill.MeasureText(Text);
    }
}
