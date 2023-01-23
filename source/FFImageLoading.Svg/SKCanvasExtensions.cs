using System;
using System.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
    internal static class SKCanvasExtensions
    {
        public static void DrawRoundRect(this SKCanvas canvas, SKRoundedRect rect, SKPaint paint)
        {
            canvas.DrawRoundRect(rect.Rect, rect.RadiusX, rect.RadiusY, paint);
        }

        public static void DrawOval(this SKCanvas canvas, SKOval oval, SKPaint paint)
        {
            canvas.DrawOval(oval.Center.X, oval.Center.Y, oval.RadiusX, oval.RadiusY, paint);
        }

        public static void DrawCircle(this SKCanvas canvas, SKCircle circle, SKPaint paint)
        {
            canvas.DrawCircle(circle.Center.X, circle.Center.Y, circle.Radius, paint);
        }

        public static void DrawLine(this SKCanvas canvas, SKLine line, SKPaint paint)
        {
            canvas.DrawLine(line.P1.X, line.P1.Y, line.P2.X, line.P2.Y, paint);
        }

        public static void DrawText(this SKCanvas canvas, SKText text)
        {
            var currentX = text.Location.X;
            var currentY = text.Location.Y;

            var textWidth = text.MeasureTextWidth();

            // For correct alignment of the complete text, we calculate its starting x-position based on the alignment
            // and draw the complete text starting from that point
            switch (text.TextAlign)
            {
                case SKTextAlign.Left:
                    // currentX is correct position
                    break;
                case SKTextAlign.Center:
                    currentX -= textWidth / 2;
                    break;
                case SKTextAlign.Right:
                    currentX -= textWidth;
                    break;
                default:
                    break;
            }

            foreach (var span in text)
            {
                currentY = span?.Y ?? currentY;
                currentX = span?.X ?? currentX;

                // we need to subtract baseline shift from currentY, since negative value causes shift to bottom in svg
                canvas.DrawText(span.Text, currentX, currentY - span?.BaselineShift ?? 0, span.Fill);

				if (span.Stroke != null)
					canvas.DrawText(span.Text, currentX, currentY - span?.BaselineShift ?? 0, span.Stroke);

				currentX += span.MeasureTextWidth();
            }
        }
    }
}
