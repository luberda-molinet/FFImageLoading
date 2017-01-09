using System;
using FFImageLoading.Work;
using System.Linq;
using System.Collections.Generic;

namespace FFImageLoading.Concurrency
{
    public class PendingTasksQueue : SimplePriorityQueue<IImageLoaderTask, int>
    {
        public override void Remove(IImageLoaderTask item)
        {
            lock (_queue)
            {
                var comparer = EqualityComparer<IImageLoaderTask>.Default;
                var found = _queue.FirstOrDefault(v => comparer.Equals(v.Data, item));

                if (found != null)
                {
                    _queue.Remove(found);
                }
            }
        }

        public IImageLoaderTask FirstOrDefaultByRawKey(string rawKey)
        {
            lock (_queue)
            {
                return _queue.FirstOrDefault(v => v.Data?.KeyRaw == rawKey)?.Data;
            }
        }

        public void CancelWhenUsesSameNativeControl(IImageLoaderTask task)
        {
            lock (_queue)
            {
                foreach (var item in _queue)
                {
                    if (item.Data != null && item.Data.UsesSameNativeControl(task))
                        item.Data.CancelIfNeeded();
                }
            }
        }
    }
}
