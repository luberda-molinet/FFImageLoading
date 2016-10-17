using System;
using System.Threading.Tasks;
using System.IO;
using FFImageLoading.Work;
using FFImageLoading.Config;
using System.Threading;

namespace FFImageLoading
{
    public interface IDataResolver
    {
        Task<Tuple<Stream, LoadingResult>> Resolve(string identifier, TaskParameter parameters, Configuration configuration, CancellationToken token);
    }
}
