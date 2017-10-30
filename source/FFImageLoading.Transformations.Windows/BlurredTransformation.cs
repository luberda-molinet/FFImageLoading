using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class BlurredTransformation : TransformationBase
    {
        public BlurredTransformation()
        {
            Radius = 20d;
        }

        public BlurredTransformation(double radius)
        {
            Radius = radius;
        }

        public double Radius { get; set; }

        public override string Key
        {
            get { return string.Format("BlurredTransformation,radius={0}", Radius); }
        }

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, ImageSource source, bool isPlaceholder, string key)
        {

            ToLegacyBlurred(bitmapSource, (int)Radius);

            return bitmapSource;
        }

        // Source: http://incubator.quasimondo.com/processing/superfast_blur.php
        public static void ToLegacyBlurred(BitmapHolder source, int radius)
        {
            int w = source.Width;
            int h = source.Height;
            int wm = w - 1;
            int hm = h - 1;
            int wh = w * h;
            int div = radius + radius + 1;
            int[] r = new int[wh];
            int[] g = new int[wh];
            int[] b = new int[wh];
            int rsum, gsum, bsum, x, y, i, yp, yi, yw;
            int[] vmin = new int[Math.Max(w, h)];
            int[] vmax = new int[Math.Max(w, h)];

            int[] dv = new int[256 * div];
            for (i = 0; i < 256 * div; i++)
            {
                dv[i] = (i / div);
            }

            yw = yi = 0;

            for (y = 0; y < h; y++)
            {
                rsum = gsum = bsum = 0;
                for (i = -radius; i <= radius; i++)
                {
                    var p = source.GetPixel(yi + Math.Min(wm, Math.Max(i, 0)));
                    rsum += p.R;
                    gsum += p.G;
                    bsum += p.B;
                }
                for (x = 0; x < w; x++)
                {

                    r[yi] = dv[rsum];
                    g[yi] = dv[gsum];
                    b[yi] = dv[bsum];

                    if (y == 0)
                    {
                        vmin[x] = Math.Min(x + radius + 1, wm);
                        vmax[x] = Math.Max(x - radius, 0);
                    }
                    var p1 = source.GetPixel(yw + vmin[x]);
                    var p2 = source.GetPixel(yw + vmax[x]);

                    rsum += p1.R - p2.R;
                    gsum += p1.G - p2.G;
                    bsum += p1.B - p2.B;
                    yi++;
                }
                yw += w;
            }

            for (x = 0; x < w; x++)
            {
                rsum = gsum = bsum = 0;
                yp = -radius * w;
                for (i = -radius; i <= radius; i++)
                {
                    yi = Math.Max(0, yp) + x;
                    rsum += r[yi];
                    gsum += g[yi];
                    bsum += b[yi];
                    yp += w;
                }
                yi = x;
                for (y = 0; y < h; y++)
                {
                    // Preserve alpha channel: ( 0xff000000 & pix[yi] )
                    var oldColor = source.GetPixel(yi);
                    var newColor = new ColorHolder(oldColor.A, dv[rsum], dv[gsum], dv[bsum]);
                    source.SetPixel(yi, newColor);
                    if (x == 0)
                    {
                        vmin[y] = Math.Min(y + radius + 1, hm) * w;
                        vmax[y] = Math.Max(y - radius, 0) * w;
                    }
                    var p1 = x + vmin[y];
                    var p2 = x + vmax[y];

                    rsum += r[p1] - r[p2];
                    gsum += g[p1] - g[p2];
                    bsum += b[p1] - b[p2];

                    yi += w;
                }
            }
        }
    }
}
