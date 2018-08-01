using FFImageLoading.Concurrency;
using FFImageLoading.Work;
using Moq;
using System;
using Xunit;

namespace FFImageLoading.Tests.Concurrency
{
    public class GenericPriorityQueue_Test
    {
        [Fact]
        public void Given_instance_created_Then_empty()
        {
            var sut = CreatePriorityQueue(5);
            Assert.Equal(0, sut.Count);
            Assert.Equal(5, sut.MaxSize);
        }

        [Fact]
        public void Given_instance_created_with_invalid_max_nodes_Then_throws()
        {
            Assert.Throws<InvalidOperationException>(() => CreatePriorityQueue(0));
        }

        [Fact]
        public void Given_item_added_Then_count_is_1()
        {
            var request = new Mock<IImageLoaderTask>().Object;
            var sut = CreatePriorityQueue();
            sut.Enqueue(CreateRequest(), 0);

            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public void Given_queue_has_items_Then_dequeue_max_priority()
        {
            var request0 = CreateRequest();
            var request1 = CreateRequest();
            var request2 = CreateRequest();
            var request3 = CreateRequest();
            var sut = CreatePriorityQueue();
            sut.Enqueue(request1, 1);
            sut.Enqueue(request0, 0);
            sut.Enqueue(request3, 3);
            sut.Enqueue(request2, 2);
            var result = sut.Dequeue();

            Assert.Same(request3, result);
        }

        [Fact]
        public void Given_queue_has_items_with_same_priority_Then_dequeue_first_enqueued()
        {
            var request0 = CreateRequest();
            var request1 = CreateRequest();
            var request2_tie1 = CreateRequest();
            var request2_tie2 = CreateRequest();
            var sut = CreatePriorityQueue();
            sut.Enqueue(request0, 0);
            sut.Enqueue(request2_tie1, 2);
            sut.Enqueue(request2_tie2, 2);
            sut.Enqueue(request1, 1);
            var result = sut.Dequeue();

            Assert.Same(request2_tie1, result);
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
            var sut = CreatePriorityQueue();
            sut.Enqueue(CreateRequest(), 0);
            sut.Enqueue(CreateRequest(), 0);
            sut.Enqueue(CreateRequest(), 0);
            sut.Dequeue();

            Assert.Equal(2, sut.Count);
        }

        [Fact]
        public void Given_first_called_Then_nothing_dequeued()
        {
            var request = CreateRequest();
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);
            var first = sut.First;

            Assert.Equal(1, sut.Count);
        }

        [Fact]
        public void Given_multiple_items_Then_first_gives_item_with_max_priority()
        {
            var request0 = CreateRequest();
            var request1 = CreateRequest();
            var request2 = CreateRequest();
            var request3 = CreateRequest();
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
            var request0 = CreateRequest();
            var request1 = CreateRequest();
            var request2 = CreateRequest();
            var request3 = CreateRequest();
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
            var request = CreateRequest();
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
            var request = CreateRequest();
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);

            Assert.True(sut.Contains(request));
        }

        [Fact]
        public void Given_item_not_in_list_Then_false()
        {
            var request = CreateRequest();
            var sut = CreatePriorityQueue();

            Assert.False(sut.Contains(request));
        }

        [Fact]
        public void Given_item_in_list_Then_removed()
        {
            var request = CreateRequest();
            var sut = CreatePriorityQueue();
            sut.Enqueue(request, 0);
            sut.Remove(request);

            Assert.Equal(0, sut.Count);
        }

        [Fact]
        public void Given_item_not_in_list_Then_remove_throws()
        {
            var request = CreateRequest();
            var sut = CreatePriorityQueue();
            Assert.Throws<InvalidOperationException>(() => sut.Remove(request));
        }

        [Fact]
        public void Given_priority_changed_Then_becomes_first()
        {
            var requestLow = CreateRequest();
            var requestHigh = CreateRequest();
            var sut = CreatePriorityQueue();
            sut.Enqueue(requestLow, 0);
            sut.Enqueue(requestHigh, 1);
            sut.UpdatePriority(requestLow, 2);

            Assert.Same(requestLow, sut.First);
        }

        [Fact]
        public void Given_priority_changed_Then_no_longer_first()
        {
            var requestLow = CreateRequest();
            var requestHigh = CreateRequest();
            var sut = CreatePriorityQueue();
            sut.Enqueue(requestLow, 0);
            sut.Enqueue(requestHigh, 1);
            sut.UpdatePriority(requestHigh, -1);

            Assert.Same(requestLow, sut.First);
        }

        //[Fact]
        //public void Given_items_Then_enumerate_by_priority()
        //{
        //    var request4 = CreateRequest();
        //    var request7 = CreateRequest();
        //    var request5 = CreateRequest();
        //    var requests = new[] { request7, request5, request4 };
        //    var sut = CreatePriorityQueue();
        //    sut.Enqueue(request4, 4);
        //    sut.Enqueue(request7, 7);
        //    sut.Enqueue(request5, 5);

        //    int i = 0;
        //    foreach (var requestEnumerated in sut)
        //    {
        //        var current = requests[i];
        //        Assert.Same(current, requestEnumerated);
        //        i++;
        //    }
        //}

        [Fact]
        public void Given_queue_resized_with_invalid_value_Then_throws()
        {
            var sut = CreatePriorityQueue(5);
            Assert.Throws<InvalidOperationException>(() => sut.Resize(0));
        }

        [Fact]
        public void Given_queue_having_items_resized_with_invalid_value_Then_throws()
        {
            var sut = CreatePriorityQueue(5);
            sut.Enqueue(CreateRequest(), 0);
            sut.Enqueue(CreateRequest(), 0);
            Assert.Throws<InvalidOperationException>(() => sut.Resize(1));
        }

        [Fact]
        public void Given_queue_resized_Then_max_size_increased()
        {
            var sut = CreatePriorityQueue(5);
            sut.Resize(10);

            Assert.Equal(10, sut.MaxSize);
        }

        private GenericPriorityQueue<SimpleNode<IImageLoaderTask, int>, int> CreatePriorityQueue(int maxItems = 10)
        {
            return new GenericPriorityQueue<SimpleNode<IImageLoaderTask, int>, int>(maxItems);
        }

        private SimpleNode<IImageLoaderTask, int> CreateRequest()
        {
            return new SimpleNode<IImageLoaderTask, int>(new Mock<IImageLoaderTask>().Object);
        }

        class SimpleNode<TItem, TPriority> : GenericPriorityQueueNode<TPriority>
        {
            public TItem Data { get; private set; }

            public SimpleNode(TItem data)
            {
                Data = data;
            }
        }
    }
}
