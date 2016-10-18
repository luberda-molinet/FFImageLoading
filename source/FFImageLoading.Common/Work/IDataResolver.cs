using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace FFImageLoading.Work
{
    public interface IDataResolver
    {
        Task<Tuple<Stream, LoadingResult, ImageInformation, DownloadInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token);
    }
}
