using Android.App;
using Java.Lang;

namespace FFImageLoading
{
    public class PlatformPerformance : IPlatformPerformance
    {
        private readonly Runtime _runtime;
        private readonly ActivityManager _activityManager;
        private readonly ActivityManager.MemoryInfo _memoryInfo;

        public PlatformPerformance()
        {
            _runtime = Runtime.GetRuntime();
            _activityManager = (ActivityManager)Application.Context.GetSystemService("activity");
            _memoryInfo = new ActivityManager.MemoryInfo();
        }

        public int GetCurrentManagedThreadId()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public int GetCurrentSystemThreadId()
        {
            return Android.OS.Process.MyTid();
        }

        public string GetMemoryInfo()
        {
            _activityManager.GetMemoryInfo(_memoryInfo);
            var availableMegs = (float)_memoryInfo.AvailMem / 1048576f;
            var totalMegs = (float)_memoryInfo.TotalMem / 1048576f;
            var percentAvail = (float)_memoryInfo.AvailMem / _memoryInfo.TotalMem * 100f;

            var availableMegsHeap = ((float)(_runtime.TotalMemory() - _runtime.FreeMemory())) / 1048576f;
            var totalMegsHeap = (float)_runtime.MaxMemory() / 1048576f;
            var percentAvailHeap = (float)(_runtime.TotalMemory() - _runtime.FreeMemory()) / _runtime.MaxMemory() * 100f;

            return string.Format("[PERFORMANCE] Memory - Free: {0:0}MB ({1:0}%), Total: {2:0}MB, Heap - Free: {3:0}MB ({4:0}%), Total: {5:0}MB",
                                 availableMegs, percentAvail, totalMegs, availableMegsHeap, percentAvailHeap, totalMegsHeap);
        }
    }
}

