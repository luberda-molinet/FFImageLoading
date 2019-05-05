using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FFImageLoading;
using FFImageLoading.Work;
using FFImageLoading.Cache;
using System.Linq;

namespace FFImageLoading.Tests.ImageServiceTests
{
    public class ImageServiceBaseTests : BaseTests
    {
        public ImageServiceBaseTests()
        {
            ImageService.EnableMockImageService = true;
			ImageService.Instance.Initialize(new Config.Configuration()
			{

				HttpHeadersTimeout = 15,
				HttpReadTimeout = 40
			});
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

        //[Fact]
        //public async Task CanDownloadOnly()
        //{
  //          await ImageService.Instance.InvalidateCacheAsync(CacheType.All);
        //	await ImageService.Instance.LoadUrl(RemoteImage)
        //		.DownloadOnlyAsync();

        //	var diskCacheKey = ImageService.Instance.Config.MD5Helper.MD5(RemoteImage);
        //	var cachedDisk = await ImageService.Instance.Config.DiskCache.ExistsAsync(diskCacheKey);
        //	Assert.True(cachedDisk);
        //}

        [Fact]
        public async Task CanPreload()
        {
            await ImageService.Instance.InvalidateCacheAsync(CacheType.All);
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

            await ImageService.Instance.InvalidateCacheAsync(CacheType.All);

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

            await ImageService.Instance.InvalidateCacheEntryAsync(RemoteImage, CacheType.All, true);

            var diskCacheKey = ImageService.Instance.Config.MD5Helper.MD5(RemoteImage);
            var cachedDisk = await ImageService.Instance.Config.DiskCache.ExistsAsync(diskCacheKey);
            Assert.False(cachedDisk);

            var cachedMemory = Mock.MockImageCache.Instance.Get(RemoteImage);
            Assert.Null(cachedMemory);
        }

        //[Fact]
        //public async Task CanPreloadMultipleUrlImageSources()
        //{
        //    await ImageService.Instance.InvalidateCacheAsync(CacheType.All);

        //    IList<Task> tasks = new List<Task>();
        //    int downloadsCount = 0;
        //    int successCount = 0;

        //    for (int i = 0; i < 5; i++)
        //    {
        //        tasks.Add(ImageService.Instance.LoadUrl(GetRandomImageUrl())
        //                  .DownloadStarted((obj) =>
        //                  {
        //                      downloadsCount++;
        //                  })
        //                  .Success((arg1, arg2) =>
        //                  {
        //                      successCount++;
        //                  })                          
        //                  .PreloadAsync());
        //    }

        //    await Task.WhenAll(tasks);
        //    Assert.Equal(5, downloadsCount);
        //    Assert.Equal(5, successCount);
        //}

        [Fact]
        public async Task CanWaitForSameUrlImageSources()
        {
            await ImageService.Instance.InvalidateCacheAsync(CacheType.All);

            IList<Task> tasks = new List<Task>();
            int downloadsCount = 0;
            int successCount = 0;

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(ImageService.Instance.LoadUrl(Images.Last())
                          .DownloadStarted((obj) =>
                          {
                              downloadsCount++;
                          })
                          .Success((arg1, arg2) =>
                          {
                              successCount++;
                          })
                          .PreloadAsync());
            }

            await Task.WhenAll(tasks);
            Assert.Equal(1, downloadsCount);
            Assert.Equal(5, successCount);
        }
    }
}
