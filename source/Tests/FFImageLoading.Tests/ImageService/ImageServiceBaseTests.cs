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

        static Random _random = new Random();
        const string RemoteImage = "https://upload.wikimedia.org/wikipedia/en/a/a9/Example.jpg";

        static string[] Images = {
                "https://farm9.staticflickr.com/8625/15806486058_7005d77438.jpg",
                "https://farm5.staticflickr.com/4011/4308181244_5ac3f8239b.jpg",
                "https://farm8.staticflickr.com/7423/8729135907_79599de8d8.jpg",
                "https://farm3.staticflickr.com/2475/4058009019_ecf305f546.jpg",
                "https://farm6.staticflickr.com/5117/14045101350_113edbe20b.jpg",
                "https://farm2.staticflickr.com/1227/1116750115_b66dc3830e.jpg",
                "https://farm8.staticflickr.com/7351/16355627795_204bf423e9.jpg",
                "https://farm1.staticflickr.com/44/117598011_250aa8ffb1.jpg",
                "https://farm8.staticflickr.com/7524/15620725287_3357e9db03.jpg",
                "https://farm9.staticflickr.com/8351/8299022203_de0cb894b0.jpg",
                "https://farm4.staticflickr.com/3688/10684479284_211f2a8b0f.jpg",
                "https://farm6.staticflickr.com/5755/20725502975_0dd9b4c5f2.jpg",
                "https://farm4.staticflickr.com/3732/9308209014_ea8eac4387.jpg",
                "https://farm4.staticflickr.com/3026/3096284216_8b2e8da102.jpg",
                "https://farm3.staticflickr.com/2915/14139578975_42d87d2d00.jpg",
                "https://farm4.staticflickr.com/3900/14949063062_a92fc5426f.jpg",
                "https://farm9.staticflickr.com/8514/8349332314_e1ae376fd4.jpg",
                "https://farm3.staticflickr.com/2241/2513217764_740fdd6afa.jpg",
                "https://farm6.staticflickr.com/5083/5377978827_51d978d271.jpg",
                "https://farm4.staticflickr.com/3626/3499605313_a9d43c1c83.jpg",
                "https://farm1.staticflickr.com/16/19438696_f103861437.jpg",
                "https://farm3.staticflickr.com/2221/2243980018_d2925f3d77.jpg",
                "https://farm8.staticflickr.com/7338/8719134406_74a21b617c.jpg",
                "https://farm6.staticflickr.com/5149/5626285743_ae6a75dde7.jpg",
                "https://farm5.staticflickr.com/4105/4963731276_a10e1bd520.jpg",
                "https://farm4.staticflickr.com/3270/2814060518_6305796eb1.jpg",
                "https://farm7.staticflickr.com/6183/6123785115_2c17b85328.jpg",
                "https://farm3.staticflickr.com/2900/14398989204_59f60a05c5.jpg",
                "https://farm3.staticflickr.com/2136/1756449787_c93e6eb647.jpg",
                "https://farm4.staticflickr.com/3201/3070391067_c80fb9e942.jpg"
            };

        public static string GetRandomImageUrl()
        {
            int i = _random.Next(Images.Length - 1);
            return Images[i];
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
