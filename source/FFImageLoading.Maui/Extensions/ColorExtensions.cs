
namespace FFImageLoading.Maui
{
    public static class ColorExtensions
    {
        public static float[][] ColorToMatrix(this Color color, float? brightness = null)
        {
            // Information can be found here: https://developer.android.com/reference/android/graphics/ColorMatrix.html

            var r = (float)color.Red;
            var g = (float)color.Green;
            var b = (float)color.Blue;

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
