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
            await ImageService.Instance.LoadUrl("https://upload.wikimedia.org/wikipedia/en/a/a9/Example.jpg")
                .DownloadOnlyAsync();
        }

        [Fact]
        public async Task CanPreload()
        {
            await ImageService.Instance.LoadUrl("https://upload.wikimedia.org/wikipedia/en/a/a9/Example.jpg")
                .PreloadAsync();
        }
    }
}
