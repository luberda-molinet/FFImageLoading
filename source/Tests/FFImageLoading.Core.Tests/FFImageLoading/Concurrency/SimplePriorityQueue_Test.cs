using FFImageLoading.Concurrency;
using FFImageLoading.Work;
using Moq;
using Xunit;

namespace FFImageLoading.Core.Tests.FFImageLoading.Concurrency
{
    public class SimplePriorityQueue_Test
    {
        [Fact]
        public void Given_instance_created_Then_empty()
        {
            var sut = CreatePriorityQueue();
            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public void Given_item_added_Then_count_is_1()
        {
            var request = new Mock<IImageLoaderTask>();
            var sut = CreatePriorityQueue();
            sut.Enqueue(request.Object, 0);

            Assert.Equal(1, sut.Count);
        }

        private IFixedSizePriorityQueue<SimpleNode<IImageLoaderTask, int>, int> CreateQueue()
        {
            var mock = new Mock<IFixedSizePriorityQueue<SimpleNode<IImageLoaderTask, int>, int>>();
            return mock.Object;
        }

        private SimplePriorityQueue<IImageLoaderTask, int> CreatePriorityQueue()
        {
            return new SimplePriorityQueue<IImageLoaderTask, int>(CreateQueue());
        }
    }
}
