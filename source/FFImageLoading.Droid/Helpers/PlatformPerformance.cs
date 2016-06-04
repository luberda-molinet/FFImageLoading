using System;
using System.Threading;
using Android.App;
using Java.Lang;

namespace FFImageLoading
{
    public class PlatformPerformance : IPlatformPerformance
    {
        Runtime _runtime;
        ActivityManager _activityManager;
        ActivityManager.MemoryInfo _memoryInfo;

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
            double availableMegs = (double)_memoryInfo.AvailMem / 1048576d;
            double totalMegs = (double)_memoryInfo.TotalMem / 1048576d;
            double percentAvail = (double)_memoryInfo.AvailMem / _memoryInfo.TotalMem * 100d;

            double availableMegsHeap = ((double)(_runtime.TotalMemory() - _runtime.FreeMemory())) / 1048576d;
            double totalMegsHeap = (double)_runtime.MaxMemory() / 1048576d;
            double percentAvailHeap = (double)(_runtime.TotalMemory() - _runtime.FreeMemory()) / _runtime.MaxMemory() * 100d;

            return string.Format("[PERFORMANCE] Memory - Free: {0:0}MB ({1:0}%), Total: {2:0}MB, Heap - Free: {3:0}MB ({4:0}%), Total: {5:0}MB", 
                                 availableMegs, percentAvail, totalMegs, availableMegsHeap, percentAvailHeap, totalMegsHeap);
        }
    }
}

