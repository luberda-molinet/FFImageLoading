using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal class SKText : IEnumerable<SKTextSpan>
    {
        private readonly List<SKTextSpan> _spans = new List<SKTextSpan>();

        public SKTextAlign TextAlign { get; }

        public SKText(SKTextAlign textAlign)
        {
            TextAlign = textAlign;
        }

        public void Append(SKTextSpan span)
        {
            _spans.Add(span);
        }

        public float MeasureTextWidth()
        {
            return _spans.Sum(x => x.Fill.MeasureText(x.Text));
        }

        //public SKRect GetBoundingRect()
        //{
        //}

        public IEnumerator<SKTextSpan> GetEnumerator() => _spans.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
