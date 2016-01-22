using FFImageLoading.Work;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FFImageLoading.DataResolver
{
    public interface IStreamResolver : IDisposable
    {
        Task<WithLoadingResult<Stream>> GetStream(string identifier, CancellationToken token);
    }
}
