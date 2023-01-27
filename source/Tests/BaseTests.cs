using System;
using FFImageLoading.Mock;

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

		public BaseTests()
		{
            var config = new Config.Configuration();
            var md5 = new MockMD5Helper();
            var diskCache = new MockDiskCache();
            var logger = new MockLogger();

            var downloadCache = new FFImageLoading.Cache.DownloadCache(
                config, md5, diskCache, logger);

            var dataResolver = new MockDataResolverFactory(config, downloadCache);

            var scheduler = new Work.WorkScheduler(config, logger, null);

            ImageService = new ImageService(
                config, md5, logger, null, new MockMainThreadDispatcher(),
                dataResolver, downloadCache, scheduler);

			ImageService.Initialize();
		}

		protected readonly IImageService<MockBitmap> ImageService;

		protected const string RemoteImage = "https://loremflickr.com/320/240/nature?random=0";
        protected static string[] Images { get; private set; }
        protected static string GetRandomImageUrl() => $"https://loremflickr.com/320/240/nature?random={Guid.NewGuid()}";
    }
}
