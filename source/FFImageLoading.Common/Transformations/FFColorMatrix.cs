using System;

namespace FFImageLoading.Transformations
{
    // http://docs.rainmeter.net/tips/colormatrix-guide
    public static class FFColorMatrix
    {
        public static float[][] GrayscaleColorMatrix = new float[][] {
            new [] { 0.33f, 0.33f, 0.33f, 0f, 0f },
            new [] { 0.59f, 0.59f, 0.59f, 0f, 0f },
            new [] { 0.11f, 0.11f, 0.11f, 0f, 0f },
            new [] { 0f, 0f, 0f, 1f, 0f },
            new [] { 0f, 0f, 0f, 0f, 1f },
        };

        public static float[][] InvertColorMatrix = new float[][] {
            new [] { -1f, 0f, 0f, 0f, 0f },
            new [] { 0f, -1f, 0f, 0f, 0f },
            new [] { 0f, 0f, -1f, 0f, 0f },
            new [] { 1f, 1f, 1f, 1f, 0f  },
            new [] { 1f, 1f, 1f, 0f, 1f },
        };

        public static float[][] SepiaColorMatrix = new float[][] {
            new [] { 0.393f, 0.349f, 0.272f, 0f, 0f },
            new [] { 0.769f, 0.686f, 0.534f, 0f, 0f },
            new [] { 0.189f, 0.168f, 0.131f, 0f, 0f },
            new [] { 0f, 0f, 0f, 1f, 0f },
            new [] { 0f, 0f, 0f, 0f, 1f },
        };

        public static float[][] BlackAndWhiteColorMatrix = new float[][] {
            new [] { 1.5f, 1.5f, 1.5f, 0f, 0 },
            new [] { 1.5f, 1.5f, 1.5f, 0f, 0 },
            new [] { 1.5f, 1.5f, 1.5f, 0f, 0 },
            new [] { 0f, 0f, 0f, 1f, 0f },
            new [] { -1f, -1f, -1f, 0f, 1 },
        };

        public static float[][] PolaroidColorMatrix = new float[][] {
            new [] { 1.438f, -0.062f, -0.062f, 0f, 0 },
            new [] { -0.122f, 1.378f, -0.122f, 0f, 0 },
            new [] { -0.016f, -0.016f, 1.483f, 0f, 0 },
            new [] { 0f, 0f, 0f, 1f, 0f },
            new [] { -0.03f, 0.05f, -0.02f, 0f, 1 },
        };

        public static float[][] WhiteToAlphaColorMatrix = new float[][] {
            new [] { 1f, 0f, 0f, -1f, 0 },
            new [] { 0f, 1f, 0f, -1f, 0 },
            new [] { 0f, 0f, 1f, -1f, 0 },
            new [] { 0f, 0f, 0f, 1f, 0f },
            new [] { 0f, 0f, 0f, 0f, 1f },
        };

        public static float[][] ColorToTintMatrix(int r, int g, int b, int a)
        {
            float progressR = (float)r / 128f;
            float progressG = (float)g / 128f;
            float progressB = (float)b / 128f;
            float progressA = (float)a / 128f;

            return new float[][]
            {
                new [] { progressR, 0f, 0f, 0f, 0f },
                new [] { 0f, progressG, 0f, 0f, 0f },
                new [] { 0f, 0f, progressB, 0f, 0f },
                new [] { 0f, 0f, 0f, progressA, 0f },
                new [] { 0f, 0f, 0f, 0f, 1f },
            };
        }
    }
}

