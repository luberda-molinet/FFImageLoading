using System;
#if __MACOS__
using AppKit;
using PColor = AppKit.NSColor;
#elif __IOS__
using UIKit;
using PColor = UIKit.UIColor;
#endif

namespace FFImageLoading
{
    public static class ColorExtensions
    {
        public static PColor ToUIColor(this string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
                throw new ArgumentException("Invalid color string.", nameof(hexColor));

            if (!hexColor.StartsWith("#", StringComparison.Ordinal))
                hexColor = hexColor.Insert(0, "#");

            var color = PColor.Clear;

            switch (hexColor.Length)
            {
                case 9:
                    {
                        var cuint = Convert.ToUInt32(hexColor.Substring(1), 16);
                        var a = (byte)(cuint >> 24);
                        var r = (byte)((cuint >> 16) & 0xff);
                        var g = (byte)((cuint >> 8) & 0xff);
                        var b = (byte)(cuint & 0xff);
#if __MACOS__
                        color = PColor.FromRgba(r, g, b, a);
#elif __IOS__
                        color = PColor.FromRGBA(r, g, b, a);
#endif
                        break;
                    }
                case 7:
                    {
                        var cuint = Convert.ToUInt32(hexColor.Substring(1), 16);
                        var r = (byte)((cuint >> 16) & 0xff);
                        var g = (byte)((cuint >> 8) & 0xff);
                        var b = (byte)(cuint & 0xff);
#if __MACOS__
                        color = PColor.FromRgba(r, g, b, (byte)255);
#elif __IOS__
                        color = PColor.FromRGBA(r, g, b, (byte)255);
#endif
                        break;
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
#if __MACOS__
                        color = PColor.FromRgba(r, g, b, a);
#elif __IOS__
                        color = PColor.FromRGBA(r, g, b, a);
#endif
                        break;
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

#if __MACOS__
                        color = PColor.FromRgba(r, g, b, (byte)255);
#elif __IOS__
                        color = PColor.FromRGBA(r, g, b, (byte)255);
#endif
                        break;
                    }
                default:
                    throw new FormatException(string.Format("The {0} string passed in the c argument is not a recognized Color format.", hexColor));
            }

            return color;
        }

    }
}

