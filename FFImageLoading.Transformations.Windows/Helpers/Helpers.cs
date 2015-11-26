using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace FFImageLoading.Transformations
{
    public static class Helpers
    {
        public const int SizeOfArgb = 4;

        public static int ToInt(this Color color)
        {
            var col = 0;

            if (color.A != 0)
            {
                var a = color.A + 1;
                col = (color.A << 24)
                  | ((byte)((color.R * a) >> 8) << 16)
                  | ((byte)((color.G * a) >> 8) << 8)
                  | ((byte)((color.B * a) >> 8));
            }

            return col;
        }

        public static void BlockCopy(Array src, int srcOffset, Array dest, int destOffset, int count)
        {
            Buffer.BlockCopy(src, srcOffset, dest, destOffset, count);
        }
    }
}
