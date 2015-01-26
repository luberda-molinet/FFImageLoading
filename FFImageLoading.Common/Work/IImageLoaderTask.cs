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

        Task RunAsync();

        Task<bool> TryLoadingFromCacheAsync();

        void Prepare();
    }
}

