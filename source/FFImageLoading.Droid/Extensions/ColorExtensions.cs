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
                hexColor.Insert(0, "#");

            Color color = Color.Transparent;

            try
            {
                color = Color.ParseColor(hexColor);
            }
            catch (Exception)
            {
            }

            return color;
        }
    }
}

