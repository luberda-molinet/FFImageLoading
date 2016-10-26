using System;

namespace FFImageLoading.Work
{
    public interface IScheduledWork
    {
        void Cancel();

        bool IsCancelled { get; }

        bool IsCompleted { get; }
    }
}

