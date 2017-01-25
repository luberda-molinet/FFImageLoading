using FFImageLoading.Concurrency;
using FFImageLoading.Work;
using Moq;
using System;
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
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);

            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public void Given_queue_has_items_Then_dequeue_max_priority()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);
            var result = sut.Dequeue();

            Assert.Same(request, result);
        }

        [Fact]
        public void Given_queue_is_empty_Then_dequeue_throws()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();

            Assert.Throws<InvalidOperationException>(() => sut.Dequeue());
        }

        [Fact]
        public void Given_item_dequeued_Then_count_decreases()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);
            sut.Enqueue(request, 0);
            sut.Enqueue(request, 0);
            sut.Dequeue();

            Assert.Equal(2, sut.Count);
        }

        private SimplePriorityQueue<IImageLoaderTask, int> CreatePriorityQueue()
        {
            var queue = new StubFixedSizePriorityQueue();
            return new SimplePriorityQueue<IImageLoaderTask, int>(queue);
        }
    }
}
