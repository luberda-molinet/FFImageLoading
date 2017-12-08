using System;
using AppKit;
using CoreGraphics;

namespace FFImageLoading.Transformations
{
    public static class NSPathExtensions
    {
        public static CGPath ToCGPath(this NSBezierPath nsPath)
        {
            var cgPath = new CGPath();
            var points = null as CGPoint[];

            for (int i = 0; i < nsPath.ElementCount; i++)
            {
                var type = nsPath.ElementAt(i, out points);

                switch (type)
                {
                    case NSBezierPathElement.MoveTo:
                        cgPath.MoveToPoint(points[0]);
                        break;

                    case NSBezierPathElement.LineTo:
                        cgPath.AddLineToPoint(points[0]);
                        break;

                    case NSBezierPathElement.CurveTo:
                        cgPath.AddCurveToPoint(cp1: points[0], cp2: points[1], point: points[2]);
                        break;

                    case NSBezierPathElement.ClosePath:
                        cgPath.CloseSubpath();
                        break;
                }
            }

            return cgPath;
        }

        public static void QuadCurveToPoint(this NSBezierPath path, CGPoint point1, CGPoint point2)
        {
            //TODO FIX THAT
            //Any quadratic spline can be expressed as a cubic (where the cubic term is zero). The end points of the cubic will be the same as the quadratic's.

            //CP0 = QP0
            //CP3 = QP2

            //The two control points for the cubic are:

            //CP1 = QP0 + 2/3 *(QP1-QP0)
            //CP2 = QP2 + 2/3 *(QP1-QP2)

            //...There is a slight error introduced due to rounding, but it is unlikely to be noticeable.


            var cp0 = point2.X;
            var cp3 = point2.X;
            var cp1 = point2.X + 2 / 3 * (point2.Y - point2.X);
            var cp2 = point2.X + 2 / 3 * (point2.Y - point2.X);
            path.RelativeCurveTo(point1, new CGPoint(cp0, cp1), new CGPoint(cp2, cp3));
        }
    }
}
