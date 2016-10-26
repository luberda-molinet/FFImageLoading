using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;

namespace FFImageLoading.DataResolvers
{
    public class StreamDataResolver : IDataResolver
    {
        public async Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var imageInformation = new ImageInformation();
            var stream = await parameters.Stream?.Invoke(token);

            return new Tuple<Stream, LoadingResult, ImageInformation>(stream, LoadingResult.Stream, imageInformation);
        }
    }
}
