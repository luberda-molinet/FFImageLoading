using System;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal struct SKRadialGradient : ISKSvgFill
    {
        public SKRadialGradient(SKPoint center, float radius, float[] positions, SKColor[] colors, SKShaderTileMode tileMode, SKMatrix matrix)
        {
            Center = center;
            Radius = radius;
            Positions = positions;
            Colors = colors;
            TileMode = tileMode;
            Matrix = matrix;
        }

        public SKPoint Center { get; set; }

        public float Radius { get; set; }

        public float[] Positions { get; set; }

        public SKColor[] Colors { get; set; }

        public SKMatrix Matrix { get; set; }

        public SKShaderTileMode TileMode { get; set; }

        public SKPoint GetCenterPoint(float x, float y, float width, float height)
        {
            if (Math.Max(Center.X, Center.Y) > 1f)
                return new SKPoint(Center.X, Center.Y);

            var x0 = x + (Center.X * width);
            var y0 = y + (Center.Y * height);

            return new SKPoint((float)x0, y0);
        }

        public float GetRadius(float width, float height)
        {
            if (Radius > 1f)
                return Radius;

            return Math.Min(width, height) * Radius;
        }

        public void ApplyFill(SKPaint fill, SKRect bounds)
        {
            var centerPoint = GetCenterPoint(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
            var radius = GetRadius(bounds.Width, bounds.Height);

            var gradientShader = SKShader.CreateRadialGradient(centerPoint, radius, Colors, Positions, TileMode, Matrix);

            fill.Color = SKColors.Black;
            fill.Shader = gradientShader;
        }
    }
}
