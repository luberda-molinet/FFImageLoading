using System;


namespace FFImageLoading.Tests
{
    public class BaseTests
    {
        static BaseTests()
        {
            Images = new string[20];

            for (int i = 0; i < Images.Length; i++)
            {
                Images[i] = GetRandomImageUrl();
            }
        }

        protected const string RemoteImage = "https://loremflickr.com/320/240/nature?random=0";
        protected static string[] Images { get; private set; }
        protected static string GetRandomImageUrl() => $"https://loremflickr.com/320/240/nature?random={Guid.NewGuid()}";
    }
}
