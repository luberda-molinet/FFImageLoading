using System;

namespace FFImageLoading.Helpers
{
    public class EmptyPlatformPerformance : IPlatformPerformance
    {
        public int GetCurrentManagedThreadId()
        {
            return 0;
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
