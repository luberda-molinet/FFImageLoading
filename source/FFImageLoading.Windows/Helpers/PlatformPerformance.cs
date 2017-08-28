using System;

namespace FFImageLoading
{
    public class PlatformPerformance : IPlatformPerformance
    {
        public static IPlatformPerformance Create()
        {
            try
            {
                return new PlatformPerformance();
            }
            catch (Exception ex)
            {
                return new EmptyPlatformPerformance();
            }
        }

        private PlatformPerformance()
        {
        }

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

