using FFImageLoading.Concurrency;
using FFImageLoading.Work;
using Moq;
using Xunit;

namespace FFImageLoading.Tests.Concurrency
{
    public class PendingTasksQueue_Test
    {
        [Fact]
        public void Given_instance_created_Then_empty()
        {
            var sut = new PendingTasksQueue();
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public void Given_image_not_found_Then_Remove_does_not_fail()
        {
            var request = new Mock<IImageLoaderTask>();
            var sut = new PendingTasksQueue();

            Assert.False(sut.TryRemove(request.Object));
        }

        [Fact]
        public void Given_image_found_Then_Removed()
        {
            var request = new Mock<IImageLoaderTask>();
            var sut = new PendingTasksQueue();
            sut.Enqueue(request.Object, 0);

            sut.Remove(request.Object);
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public void Given_raw_key_Then_item_found()
        {
            var request = new Mock<IImageLoaderTask>();
            request.Setup(r => r.KeyRaw).Returns("foo");
            var request2 = new Mock<IImageLoaderTask>();
            request2.Setup(r => r.KeyRaw).Returns("bar");
            var sut = new PendingTasksQueue();
            sut.Enqueue(request.Object, 0);
            sut.Enqueue(request2.Object, 0);

            var result = sut.FirstOrDefaultByRawKey("foo");
            Assert.Equal(request.Object, result);
        }

        [Fact]
        public void Given_raw_key_Then_item_not_found()
        {
            var sut = new PendingTasksQueue();

            var result = sut.FirstOrDefaultByRawKey("foo");
            Assert.Null(result);
        }
    }
}
