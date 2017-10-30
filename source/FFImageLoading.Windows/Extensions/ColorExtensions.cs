using FFImageLoading.Helpers;
using System;
using Windows.UI;

namespace FFImageLoading.Extensions
{
    public static class ColorExtensions
    {
        public const int SizeOfArgb = 4;

        //public static int ToInt(this ColorHolder color)
        //{
        //    var col = 0;

        //    if (color.A != 0)
        //    {
        //        var a = color.A + 1;
        //        col = (color.A << 24)
        //          | ((byte)((color.R * a) >> 8) << 16)
        //          | ((byte)((color.G * a) >> 8) << 8)
        //          | ((byte)((color.B * a) >> 8));
        //    }

        //    return col;
        //}

        public static ColorHolder ToColorFromHex(this string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
                throw new ArgumentException("Invalid color string.", nameof(hexColor));

            if (!hexColor.StartsWith("#", StringComparison.Ordinal))
                hexColor.Insert(0, "#");

            switch (hexColor.Length)
            {
                case 9:
                    {
                        var cuint = Convert.ToUInt32(hexColor.Substring(1), 16);
                        var a = (byte)(cuint >> 24);
                        var r = (byte)((cuint >> 16) & 0xff);
                        var g = (byte)((cuint >> 8) & 0xff);
                        var b = (byte)(cuint & 0xff);

                        return new ColorHolder(a, r, g, b);
                    }
                case 7:
                    {
                        var cuint = Convert.ToUInt32(hexColor.Substring(1), 16);
                        var r = (byte)((cuint >> 16) & 0xff);
                        var g = (byte)((cuint >> 8) & 0xff);
                        var b = (byte)(cuint & 0xff);

                        return new ColorHolder(255, r, g, b);
                    }
                case 5:
                    {
                        var cuint = Convert.ToUInt16(hexColor.Substring(1), 16);
                        var a = (byte)(cuint >> 12);
                        var r = (byte)((cuint >> 8) & 0xf);
                        var g = (byte)((cuint >> 4) & 0xf);
                        var b = (byte)(cuint & 0xf);
                        a = (byte)(a << 4 | a);
                        r = (byte)(r << 4 | r);
                        g = (byte)(g << 4 | g);
                        b = (byte)(b << 4 | b);

                        return new ColorHolder(a, r, g, b);
                    }
                case 4:
                    {
                        var cuint = Convert.ToUInt16(hexColor.Substring(1), 16);
                        var r = (byte)((cuint >> 8) & 0xf);
                        var g = (byte)((cuint >> 4) & 0xf);
                        var b = (byte)(cuint & 0xf);
                        r = (byte)(r << 4 | r);
                        g = (byte)(g << 4 | g);
                        b = (byte)(b << 4 | b);

                        return new ColorHolder(255, r, g, b);
                    }
                default:
                    throw new FormatException(string.Format("The {0} string is not a recognized HexColor format.", hexColor));
            }
        }
    }
}
