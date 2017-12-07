using System;
using FFImageLoading.Work;
using System.Linq;
using System.Collections.Generic;

namespace FFImageLoading.Concurrency
{
    public class PendingTasksQueue : SimplePriorityQueue<IImageLoaderTask, int>
    {
        EqualityComparer<IImageLoaderTask> _comparer = EqualityComparer<IImageLoaderTask>.Default;

        public PendingTasksQueue() : base()
        {
        }

        public IImageLoaderTask FirstOrDefaultByRawKey(string rawKey)
        {
            lock (_queue)
            {
                return _queue.FirstOrDefault(v => v.Data?.KeyRaw == rawKey)?.Data;
            }
        }
    }
}
