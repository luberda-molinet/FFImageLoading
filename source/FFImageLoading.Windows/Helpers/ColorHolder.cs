using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFImageLoading
{
    public struct ColorHolder
    {
        public ColorHolder(int a, int r, int g, int b)
        {
            A = Convert.ToByte(Math.Min(Math.Max(0, a), 255));

            if (a > 0)
            {
                R = Convert.ToByte(Math.Min(Math.Max(0, r), 255));
                G = Convert.ToByte(Math.Min(Math.Max(0, g), 255));
                B = Convert.ToByte(Math.Min(Math.Max(0, b), 255));
            }
            else
            {
                R = 0;
                G = 0;
                B = 0;
            }
        }

        public ColorHolder(int r, int g, int b)
        {
            A = 255;
            R = Convert.ToByte(Math.Min(Math.Max(0, r), 255));
            G = Convert.ToByte(Math.Min(Math.Max(0, g), 255));
            B = Convert.ToByte(Math.Min(Math.Max(0, b), 255));
        }

        public byte A { get; private set; }

        public byte R { get; private set; }

        public byte G { get; private set; }

        public byte B { get; private set; }

        public static readonly ColorHolder Transparent = new ColorHolder(0, 0, 0, 0);
    }
}
