using System;
using FFImageLoading.Work;

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
    }
}
