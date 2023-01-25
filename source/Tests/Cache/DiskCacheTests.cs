using System;
using Xunit;
using System.Threading.Tasks;
using FFImageLoading;
using FFImageLoading.Mock;

namespace FFImageLoading.Tests.Cache
{
    public class DiskCacheTests : BaseTests
    {
        [Fact]
        public async Task CanAddGet()
        {
            var diskCache = ImageService.DiskCache;
            var key = Guid.NewGuid().ToString();

            byte[] bytes = new byte[] { 00, 01, 00, 01 };

            await diskCache.AddToSavingQueueIfNotExistsAsync(key, bytes, TimeSpan.FromDays(1));
            Assert.True(await diskCache.ExistsAsync(key));

            var found = await diskCache.TryGetStreamAsync(key);
            Assert.NotNull(found);

            using (found)
            {
                Assert.Equal(bytes[0], found.ReadByte());
                Assert.Equal(bytes[1], found.ReadByte());
                Assert.Equal(bytes[2], found.ReadByte());
                Assert.Equal(bytes[3], found.ReadByte());
            }
        }
    }
}
