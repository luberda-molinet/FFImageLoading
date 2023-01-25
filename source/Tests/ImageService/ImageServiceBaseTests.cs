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
        [Fact]
        public void CanInitialize()
        {
            ImageService.Initialize();
            Assert.NotNull(ImageService.Configuration);
        }

        [Fact]
        public void CanInitializeWithCustomConfig()
        {
            ImageService.Initialize(new Config.Configuration());
            Assert.NotNull(ImageService.Configuration);
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
            await ImageService.InvalidateCacheAsync(CacheType.All);
            await ImageService.LoadUrl(RemoteImage)
                .PreloadAsync(ImageService);

            var diskCacheKey = ImageService.Md5Helper.MD5(RemoteImage);
            var cachedDisk = await ImageService.DiskCache.ExistsAsync(diskCacheKey);
            Assert.True(cachedDisk);

            var cachedMemory = ImageService.MemoryCache.Get(RemoteImage);
            Assert.NotNull(cachedMemory);
        }

        [Fact]
        public async Task CanInvalidate()
        {
            await ImageService.LoadUrl(RemoteImage)
                .PreloadAsync(ImageService);

            await ImageService.InvalidateCacheAsync(CacheType.All);

            var diskCacheKey = ImageService.Md5Helper.MD5(RemoteImage);
            var cachedDisk = await ImageService.DiskCache.ExistsAsync(diskCacheKey);
            Assert.False(cachedDisk);

            var cachedMemory = ImageService.MemoryCache.Get(RemoteImage);
            Assert.Null(cachedMemory);
        }

        [Fact]
        public async Task CanInvalidateEntry()
        {
            await ImageService.LoadUrl(RemoteImage)
                .PreloadAsync(ImageService);

            await ImageService.InvalidateCacheEntryAsync(RemoteImage, CacheType.All, true);

            var diskCacheKey = ImageService.Md5Helper.MD5(RemoteImage);
            var cachedDisk = await ImageService.DiskCache.ExistsAsync(diskCacheKey);
            Assert.False(cachedDisk);

            var cachedMemory = ImageService.MemoryCache.Get(RemoteImage);
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
            await ImageService.InvalidateCacheAsync(CacheType.All);

            IList<Task> tasks = new List<Task>();
            int downloadsCount = 0;
            int successCount = 0;

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(ImageService.LoadUrl(Images.Last())
                          .DownloadStarted((obj) =>
                          {
                              downloadsCount++;
                          })
                          .Success((arg1, arg2) =>
                          {
                              successCount++;
                          })
                          .PreloadAsync(ImageService));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(1, downloadsCount);
            Assert.Equal(5, successCount);
        }
    }
}
