using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers
{
    public class PlatformPerformance : IPlatformPerformance
    {
        public PlatformPerformance()
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
