using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;

namespace FFImageLoading.DataResolvers
{
    public class StreamDataResolver : IDataResolver
    {
        public async virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var imageInformation = new ImageInformation();
            var stream = parameters.StreamRead ?? await (parameters.Stream?.Invoke(token)).ConfigureAwait(false);

            return new DataResolverResult(stream, LoadingResult.Stream, imageInformation);
        }
    }
}
