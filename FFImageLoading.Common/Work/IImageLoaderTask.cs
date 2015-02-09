using System;
using System.Threading.Tasks;

namespace FFImageLoading.Work
{
    public interface IImageLoaderTask: IScheduledWork
    {
        string Key { get; }

        TaskParameter Parameters { get; }

        Task RunAsync();

        Task<bool> TryLoadingFromCacheAsync();

        void Prepare();
    }
}

