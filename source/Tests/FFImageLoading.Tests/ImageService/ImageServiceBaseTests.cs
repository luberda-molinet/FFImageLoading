using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FFImageLoading.Tests.ImageServiceTests
{
    public class ImageServiceBaseTests
    {
        public ImageServiceBaseTests()
        {
            ImageService.EnableMockImageService = true;
        }

        const string RemoteImage = "https://upload.wikimedia.org/wikipedia/en/a/a9/Example.jpg";

        public static string GetRandomImageUrl(int width = 100, int height = 100)
        {
            return string.Format("http://loremflickr.com/{1}/{2}/nature?filename={0}.jpg",
                Guid.NewGuid().ToString("N"), width, height);
        }

        [Fact]
        public void CanInitialize()
        {
            ImageService.Instance.Initialize();
            Assert.NotNull(ImageService.Instance.Config);
        }

        [Fact]
        public void CanInitializeWithCustomConfig()
        {
            ImageService.Instance.Initialize(new Config.Configuration());
            Assert.NotNull(ImageService.Instance.Config);
        }

        [Fact]
        public async Task CanDownloadOnly()
        {
            await ImageService.Instance.LoadUrl(RemoteImage)
                .DownloadOnlyAsync();

            var diskCacheKey = ImageService.Instance.Config.MD5Helper.MD5(RemoteImage);
            var cachedDisk = await ImageService.Instance.Config.DiskCache.ExistsAsync(diskCacheKey);
            Assert.True(cachedDisk);
        }

        [Fact]
        public async Task CanPreload()
        {
            await ImageService.Instance.LoadUrl(RemoteImage)
                .PreloadAsync();

            var diskCacheKey = ImageService.Instance.Config.MD5Helper.MD5(RemoteImage);
            var cachedDisk = await ImageService.Instance.Config.DiskCache.ExistsAsync(diskCacheKey);
            Assert.True(cachedDisk);

            var cachedMemory = Mock.MockImageCache.Instance.Get(RemoteImage);
            Assert.NotNull(cachedMemory);
        }

        [Fact]
        public async Task CanInvalidate()
        {
            await ImageService.Instance.LoadUrl(RemoteImage)
                .PreloadAsync();

            await ImageService.Instance.InvalidateCacheAsync(Cache.CacheType.All);

            var diskCacheKey = ImageService.Instance.Config.MD5Helper.MD5(RemoteImage);
            var cachedDisk = await ImageService.Instance.Config.DiskCache.ExistsAsync(diskCacheKey);
            Assert.False(cachedDisk);

            var cachedMemory = Mock.MockImageCache.Instance.Get(RemoteImage);
            Assert.Null(cachedMemory);
        }

        [Fact]
        public async Task CanInvalidateEntry()
        {
            await ImageService.Instance.LoadUrl(RemoteImage)
                .PreloadAsync();

            await ImageService.Instance.InvalidateCacheEntryAsync(RemoteImage, Cache.CacheType.All, true);

            var diskCacheKey = ImageService.Instance.Config.MD5Helper.MD5(RemoteImage);
            var cachedDisk = await ImageService.Instance.Config.DiskCache.ExistsAsync(diskCacheKey);
            Assert.False(cachedDisk);

            var cachedMemory = Mock.MockImageCache.Instance.Get(RemoteImage);
            Assert.Null(cachedMemory);
        }

        [Fact]
        public async Task CanPreloadMultipleUrlImageSources()
        {
            IList<Task> tasks = new List<Task>();
            int downloadsCount = 0;

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(ImageService.Instance.LoadUrl(GetRandomImageUrl())
                          .DownloadStarted((obj) =>
                          {
                              downloadsCount++;
                          })
                          .PreloadAsync());
            }

            await Task.WhenAll(tasks);
            Assert.Equal(5, downloadsCount);
        }

        [Fact]
        public async Task CanWaitForSameUrlImageSources()
        {
            IList<Task> tasks = new List<Task>();
            int downloadsCount = 0;
            var source = GetRandomImageUrl();

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(ImageService.Instance.LoadUrl(source)
                          .DownloadStarted((obj) =>
                            {
                                downloadsCount++;
                            })
                          .PreloadAsync());
            }

            await Task.WhenAll(tasks);
            Assert.Equal(1, downloadsCount);
        }
    }
}
