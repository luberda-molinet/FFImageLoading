using System;
using System.Reflection;
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

        public static Color ToColorFromHex(this string c)
        {
            if (string.IsNullOrEmpty(c))
                throw new ArgumentException("Invalid color string.", "c");

            if (c[0] == '#')
            {
                switch (c.Length)
                {
                    case 9:
                        {
                            //var cuint = uint.Parse(c.Substring(1), NumberStyles.HexNumber);
                            var cuint = Convert.ToUInt32(c.Substring(1), 16);
                            var a = (byte)(cuint >> 24);
                            var r = (byte)((cuint >> 16) & 0xff);
                            var g = (byte)((cuint >> 8) & 0xff);
                            var b = (byte)(cuint & 0xff);

                            return Color.FromArgb(a, r, g, b);
                        }
                    case 7:
                        {
                            var cuint = Convert.ToUInt32(c.Substring(1), 16);
                            var r = (byte)((cuint >> 16) & 0xff);
                            var g = (byte)((cuint >> 8) & 0xff);
                            var b = (byte)(cuint & 0xff);

                            return Color.FromArgb(255, r, g, b);
                        }
                    case 5:
                        {
                            var cuint = Convert.ToUInt16(c.Substring(1), 16);
                            var a = (byte)(cuint >> 12);
                            var r = (byte)((cuint >> 8) & 0xf);
                            var g = (byte)((cuint >> 4) & 0xf);
                            var b = (byte)(cuint & 0xf);
                            a = (byte)(a << 4 | a);
                            r = (byte)(r << 4 | r);
                            g = (byte)(g << 4 | g);
                            b = (byte)(b << 4 | b);

                            return Color.FromArgb(a, r, g, b);
                        }
                    case 4:
                        {
                            var cuint = Convert.ToUInt16(c.Substring(1), 16);
                            var r = (byte)((cuint >> 8) & 0xf);
                            var g = (byte)((cuint >> 4) & 0xf);
                            var b = (byte)(cuint & 0xf);
                            r = (byte)(r << 4 | r);
                            g = (byte)(g << 4 | g);
                            b = (byte)(b << 4 | b);

                            return Color.FromArgb(255, r, g, b);
                        }
                    default:
                        throw new FormatException(string.Format("The {0} string passed in the c argument is not a recognized Color format.", c));
                }
            }
            else if (
                c.Length > 3 &&
                c[0] == 's' &&
                c[1] == 'c' &&
                c[2] == '#')
            {
                var values = c.Split(',');

                if (values.Length == 4)
                {
                    var scA = double.Parse(values[0].Substring(3));
                    var scR = double.Parse(values[1]);
                    var scG = double.Parse(values[2]);
                    var scB = double.Parse(values[3]);

                    return Color.FromArgb(
                        (byte)(scA * 255),
                        (byte)(scR * 255),
                        (byte)(scG * 255),
                        (byte)(scB * 255));
                }
                else if (values.Length == 3)
                {
                    var scR = double.Parse(values[0].Substring(3));
                    var scG = double.Parse(values[1]);
                    var scB = double.Parse(values[2]);

                    return Color.FromArgb(
                        255,
                        (byte)(scR * 255),
                        (byte)(scG * 255),
                        (byte)(scB * 255));
                }
                else
                {
                    throw new FormatException(string.Format("The {0} string passed in the c argument is not a recognized Color format (sc#[scA,]scR,scG,scB).", c));
                }
            }
            else
            {
                var prop = typeof(Colors).GetTypeInfo().GetDeclaredProperty(c);
                return (Color)prop.GetValue(null);
            }
        }

        public static void BlockCopy(Array src, int srcOffset, Array dest, int destOffset, int count)
        {
            Buffer.BlockCopy(src, srcOffset, dest, destOffset, count);
        }
    }
}
