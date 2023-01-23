using System;
namespace FFImageLoading
{
    [Preserve(AllMembers = true)]
    public interface IPlatformPerformance
    {
        int GetCurrentManagedThreadId();

        int GetCurrentSystemThreadId();

        string GetMemoryInfo();
    }
}

