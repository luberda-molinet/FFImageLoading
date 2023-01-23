using FFImageLoading.Work;
using System.Linq;

namespace FFImageLoading.Concurrency
{
    public class PendingTasksQueue : SimplePriorityQueue<IImageLoaderTask, int>
    {
        public IImageLoaderTask FirstOrDefaultByRawKey(string rawKey)
        {
            lock (_queue)
            {
                return _queue.FirstOrDefault(v => v.Data?.KeyRaw == rawKey)?.Data;
            }
        }
    }
}
