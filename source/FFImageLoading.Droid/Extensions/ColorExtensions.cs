using System;
using Android.Graphics;

namespace FFImageLoading
{
    public static class ColorExtensions
    {
        public static Color ToColor(this string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
                throw new ArgumentException("Invalid color string.", nameof(hexColor));

            if (!hexColor.StartsWith("#", StringComparison.Ordinal))
                hexColor = hexColor.Insert(0, "#");

            if (hexColor.Length != 4 && hexColor.Length != 5 && hexColor.Length != 7 && hexColor.Length != 9)
                 throw new FormatException(string.Format("The {0} string is not a recognized HexColor format.", hexColor));

            return Color.ParseColor(hexColor);
        }
    }
}

