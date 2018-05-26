using System;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal struct SKLinearGradient
    {
        public SKLinearGradient(float startX, float startY, float endX, float endY, float[] positions, SKColor[] colors, SKShaderTileMode tileMode)
        {
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
            Positions = positions;
            Colors = colors;
            TileMode = tileMode;
        }

        public float StartX { get; set; }

        public float StartY { get; set; }

        public float EndX { get; set; }

        public float EndY { get; set; }

        public float[] Positions { get; set; }

        public SKColor[] Colors { get; set; }

        public SKShaderTileMode TileMode { get; set; }

        public SKPoint GetStartPoint(float x, float y, float width, float height)
        {
            if (Math.Max(StartX, StartY) > 1f)
                return new SKPoint(StartX, StartY);

            var x0 = x + StartX * width;
            var y0 = y + StartY * height;

            return new SKPoint(x0, y0);
        }

        public SKPoint GetEndPoint(float x, float y, float width, float height)
        {
            if (Math.Max(EndX, EndY) > 1f)
                return new SKPoint(EndX, EndY);

            var x0 = x + EndX * width;
            var y0 = y + EndY * height;

            return new SKPoint(x0, y0);
        }
    }
}