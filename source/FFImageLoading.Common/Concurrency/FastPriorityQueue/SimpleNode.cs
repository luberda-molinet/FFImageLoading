using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFImageLoading.Concurrency
{
    public class SimpleNode<TItem, TPriority> : GenericPriorityQueueNode<TPriority>
    {
        public TItem Data { get; private set; }

        public SimpleNode(TItem data)
        {
            Data = data;
        }
    }
}
