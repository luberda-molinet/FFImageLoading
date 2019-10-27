using System;
using FFImageLoading.Work;

namespace FFImageLoading.Args
{
    public class FinishEventArgs : EventArgs
    {
        public FinishEventArgs(IScheduledWork scheduledWork)
        {
            ScheduledWork = scheduledWork;
        }

        public IScheduledWork ScheduledWork { get; private set; }
    }
}
