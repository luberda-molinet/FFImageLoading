using FFImageLoading.Concurrency;
using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace FFImageLoading.Core.Tests.FFImageLoading.Concurrency
{
    public class StubFixedSizePriorityQueue : IFixedSizePriorityQueue<SimpleNode<IImageLoaderTask, int>, int>
    {
        private readonly List<SimpleNode<IImageLoaderTask, int>> list;
        public StubFixedSizePriorityQueue()
        {
            list = new List<SimpleNode<IImageLoaderTask, int>>();
        }

        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        public SimpleNode<IImageLoaderTask, int> First
        {
            get
            {
                return list.OrderByDescending(n => n.Priority).FirstOrDefault();
            }
        }

        public int MaxSize
        {
            get
            {
                return list.Count;
            }
        }

        public void Clear()
        {
            list.Clear();
        }

        public bool Contains(SimpleNode<IImageLoaderTask, int> node)
        {
            return list.Contains(node);
        }

        public SimpleNode<IImageLoaderTask, int> Dequeue()
        {
            var node = First;
            list.Remove(node);
            return node;
        }

        public void Enqueue(SimpleNode<IImageLoaderTask, int> node, int priority)
        {
            node.Priority = priority;
            list.Add(node);
        }

        public bool IsValidQueue()
        {
            throw new NotImplementedException();
        }

        public void Remove(SimpleNode<IImageLoaderTask, int> node)
        {
            list.Remove(node);
        }

        public void Resize(int maxNodes)
        {
        }

        public void UpdatePriority(SimpleNode<IImageLoaderTask, int> node, int priority)
        {
            node.Priority = priority;
        }

        public IEnumerator<SimpleNode<IImageLoaderTask, int>> GetEnumerator()
        {
            return list.OrderByDescending(n => n.Priority).ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
