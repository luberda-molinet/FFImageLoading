using System;

namespace FFImageLoading
{
    public class PlatformPerformance : IPlatformPerformance
    {
        public PlatformPerformance()
        {
        }

        public int GetCurrentManagedThreadId()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public int GetCurrentSystemThreadId()
        {
            return 0;
        }

        public string GetMemoryInfo()
        {
            return "[PERFORMANCE] Memory - not implemented";
        }
    }
}

