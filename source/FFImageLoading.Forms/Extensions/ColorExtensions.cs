using Xamarin.Forms;

namespace FFImageLoading.Forms
{
    public static class ColorExtensions
    {
        public static float[][] ColorToMatrix(this Color color, float? brightness = null)
        {
            // Information can be found here: https://developer.android.com/reference/android/graphics/ColorMatrix.html

            var r = (float)color.R;
            var g = (float)color.G;
            var b = (float)color.B;

            var rBrightness = brightness == null ? -r / 2 : brightness.Value;
            var gBrightness = brightness == null ? -g / 2 : brightness.Value;
            var bBrightness = brightness == null ? -b / 2 : brightness.Value;

            return new float[][]
            {
                new [] { r, g, b, 0f, 0f},
                new [] { r, g, b, 0f, 0f},
                new [] { r, g, b, 0f, 0f},
                new [] { rBrightness, gBrightness, bBrightness, 1f, 0f },
                new [] { 0f, 0f, 0f, 0f, 1f },
            };
        }
    }
}
