using System;
namespace FFImageLoading.Forms.Sample
{
    public static class Helpers
    {
        public static string GetRandomImageUrl(int width = 600, int height = 600)
        {
            return string.Format("https://placeimg.com/{1}/{2}/nature?filename={0}.jpg",
                Guid.NewGuid().ToString("N"), width, height);
        }

        public static string GetRandomImageUrlAlt(int width = 600, int height = 600)
        {
            return $"https://loremflickr.com/{width}/{height}/nature?random={Guid.NewGuid()}";
        }

        public static string GetImageUrlAlt(int key, int width = 600, int height = 600)
        {
            return $"https://loremflickr.com/{width}/{height}/nature?random={key}";
        }
    }
}
