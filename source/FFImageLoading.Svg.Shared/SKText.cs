using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal class SKText : IEnumerable<SKTextSpan>
    {
        private readonly List<SKTextSpan> spans = new List<SKTextSpan>();

        public SKText(SKPoint location, SKTextAlign textAlign)
        {
            Location = location;
            TextAlign = textAlign;
        }

        public void Append(SKTextSpan span)
        {
            spans.Add(span);
        }

        public SKPoint Location { get; }

        public SKTextAlign TextAlign { get; }

        public float MeasureTextWidth() => spans.Sum(x => x.MeasureTextWidth());

        public IEnumerator<SKTextSpan> GetEnumerator() => spans.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
