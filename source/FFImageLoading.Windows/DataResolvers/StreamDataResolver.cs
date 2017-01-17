using FFImageLoading.Work;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FFImageLoading.DataResolvers
{
    public class StreamDataResolver : IDataResolver
    {
        public async virtual Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var imageInformation = new ImageInformation();
            var stream = parameters.StreamRead ?? await (parameters.Stream?.Invoke(token)).ConfigureAwait(false);

            return new Tuple<Stream, LoadingResult, ImageInformation>(stream, LoadingResult.Stream, imageInformation);
        }
    }
}
