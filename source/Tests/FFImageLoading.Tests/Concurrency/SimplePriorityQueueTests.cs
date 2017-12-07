using FFImageLoading.Concurrency;
using FFImageLoading.Work;
using Moq;
using System;
using Xunit;

namespace FFImageLoading.Tests.Concurrency
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
            var request0 = new Mock<IImageLoaderTask>().Object;
            var request1 = new Mock<IImageLoaderTask>().Object;
            var request2 = new Mock<IImageLoaderTask>().Object;
            var request3 = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request1, 1);
            sut.Enqueue(request0, 0);
            sut.Enqueue(request3, 3);
            sut.Enqueue(request2, 2);
            var result = sut.Dequeue();

            Assert.Same(request3, result);
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

        [Fact]
        public void Given_first_called_Then_nothing_dequeued()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);
            var first = sut.First;

            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public void Given_multiple_items_Then_first_gives_item_with_max_priority()
        {
            var request0 = new Mock<IImageLoaderTask>().Object;
            var request1 = new Mock<IImageLoaderTask>().Object;
            var request2 = new Mock<IImageLoaderTask>().Object;
            var request3 = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request1, 1);
            sut.Enqueue(request0, 0);
            sut.Enqueue(request3, 3);
            sut.Enqueue(request2, 2);
            var result = sut.First;

            Assert.Same(request3, result);
        }

        [Fact]
        public void Given_queue_is_empty_Then_first_throws()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();

            Assert.Throws<InvalidOperationException>(() => sut.First);
        }

        [Fact]
        public void Given_multiple_items_Then_first_and_dequeue_are_same()
        {
            var request0 = new Mock<IImageLoaderTask>().Object;
            var request1 = new Mock<IImageLoaderTask>().Object;
            var request2 = new Mock<IImageLoaderTask>().Object;
            var request3 = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request1, 1);
            sut.Enqueue(request0, 0);
            sut.Enqueue(request3, 3);
            sut.Enqueue(request2, 2);
            var first = sut.First;
            var dequeued = sut.Dequeue();

            Assert.Same(first, dequeued);
        }

        [Fact]
        public void Given_queue_cleared_Then_is_empty()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);
            sut.Clear();

            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public void Given_clear_on_empty_queue_Then_no_exception()
        {
            var sut = CreatePriorityQueue();
            sut.Clear();
        }

        [Fact]
        public void Given_item_in_list_Then_true()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);

            Assert.True(sut.Contains(request));
        }

        [Fact]
        public void Given_item_not_in_list_Then_false()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();

            Assert.False(sut.Contains(request));
        }

        [Fact]
        public void Given_item_in_list_Then_removed()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);
            sut.Remove(request);

            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public void Given_item_not_in_list_Then_remove_throws()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            Assert.Throws<InvalidOperationException>(() => sut.Remove(request));
        }

        [Fact]
        public void Given_priority_changed_Then_becomes_first()
        {
            var requestLow = new Mock<IImageLoaderTask>().Object;
            var requestHigh = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(requestLow, 0);
            sut.Enqueue(requestHigh, 1);
            sut.UpdatePriority(requestLow, 2);

            Assert.Same(requestLow, sut.First);
        }

        [Fact]
        public void Given_priority_changed_Then_no_longer_first()
        {
            var requestLow = new Mock<IImageLoaderTask>().Object;
            var requestHigh = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(requestLow, 0);
            sut.Enqueue(requestHigh, 1);
            sut.UpdatePriority(requestHigh, -1);

            Assert.Same(requestLow, sut.First);
        }

        [Fact]
        public void Given_items_Then_enqueue_by_priority()
        {
            var request4 = new Mock<IImageLoaderTask>().Object;
            var request7 = new Mock<IImageLoaderTask>().Object;
            var request5 = new Mock<IImageLoaderTask>().Object;
            
            var sut = CreatePriorityQueue();
            sut.Enqueue(request4, 4);
            sut.Enqueue(request7, 7);
            sut.Enqueue(request5, 5);

            var result1 = sut.Dequeue();
            var result2 = sut.Dequeue();
            var result3 = sut.Dequeue();

            var requests = new[] { request7, request5, request4 };
            var results = new[] { result1, result2, result3 };

            int i = 0;
            foreach (var result in results)
            {

                Assert.Same(requests[i], result);
                i++;
            }
        }

        [Fact]
        public void Given_item_trydequeued_Then_count_decreases()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);
            sut.Enqueue(request, 0);
            sut.Enqueue(request, 0);
            IImageLoaderTask item;
            var result = sut.TryDequeue(out item);

            Assert.True(result);
            Assert.Equal(request, item);

            Assert.Equal(2, sut.Count);
        }

        [Fact]
        public void Given_queue_is_empty_trydequeue_returns_false()
        {
            var sut = CreatePriorityQueue();
            IImageLoaderTask item = null;
            var result = sut.TryDequeue(out item);
            Assert.True(!result);
            Assert.Equal(default(IImageLoaderTask), item);
        }

        private SimplePriorityQueue<IImageLoaderTask, int> CreatePriorityQueue()
        {
            return new SimplePriorityQueue<IImageLoaderTask, int>();
        }
    }
}
