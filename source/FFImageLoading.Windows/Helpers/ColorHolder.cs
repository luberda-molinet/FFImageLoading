using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers
{
    public struct ColorHolder
    {
        public ColorHolder(int a, int r, int g, int b)
        {
            A = Convert.ToByte(a);
            R = Convert.ToByte(r);
            G = Convert.ToByte(g);
            B = Convert.ToByte(b);
        }

        public byte A { get; private set; }

        public byte R { get; private set; }

        public byte G { get; private set; }

        public byte B { get; private set; }
    }
}
