using System;
using FFImageLoading.Work;
using System.Linq;

namespace FFImageLoading.Concurrency
{
    public class PendingTasksQueue : SimplePriorityQueue<IImageLoaderTask, int>
    {
        public override void Remove(IImageLoaderTask item)
        {
            try
            {
                base.Remove(item);
            }
            catch (InvalidOperationException)
            {
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
