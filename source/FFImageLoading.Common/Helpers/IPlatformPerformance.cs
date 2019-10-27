using System;
namespace FFImageLoading
{
    public interface IPlatformPerformance
    {
        int GetCurrentManagedThreadId();

        int GetCurrentSystemThreadId();

        string GetMemoryInfo();
    }
}

