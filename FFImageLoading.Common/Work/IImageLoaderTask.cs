using System;
using System.Threading.Tasks;

namespace FFImageLoading.Work
{
    public interface IImageLoaderTask
    {
        string Key { get; }

        TaskParameter Parameters { get; }

        void Cancel();

        bool IsCancelled { get; }

        bool Completed { get; }

        void Prepare();

        Task RunAsync();

        Task<bool> TryLoadingFromCacheAsync();
    }
}

