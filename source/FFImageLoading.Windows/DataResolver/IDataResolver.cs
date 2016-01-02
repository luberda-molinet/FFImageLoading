using System;
using System.Threading;
using System.Threading.Tasks;

namespace FFImageLoading.DataResolver
{
    public interface IDataResolver : IDisposable
    {
        Task<ResolverImageData> GetData(string identifier, CancellationToken token);
    }
}
