using System;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal struct SKRadialGradient
    {
        public SKRadialGradient(float centerX, float centerY, float radius, float[] positions, SKColor[] colors, SKShaderTileMode tileMode)
        {
            CenterX = centerX;
            CenterY = centerY;
            Radius = radius;
            Positions = positions;
            Colors = colors;
            TileMode = tileMode;
        }

        public float CenterX { get; set; }

        public float CenterY { get; set; }

        public float Radius { get; set; }

        public float[] Positions { get; set; }

        public SKColor[] Colors { get; set; }

        public SKShaderTileMode TileMode { get; set; }

        public SKPoint GetCenterPoint(float x, float y, float width, float height)
        {
            if (Math.Max(CenterX, CenterY) > 1f)
                return new SKPoint(CenterX, CenterY);

            var x0 = x + (CenterX * width);
            var y0 = y + (CenterY * height);

            return new SKPoint((float)x0, y0);
        }

        public float GetRadius(float width, float height)
        {
            if (Radius > 1f)
                return Radius;

            return Math.Min(width, height) * Radius;
        }
    }
}