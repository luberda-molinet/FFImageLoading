using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;
using FFImageLoading.Helpers;
using System.Text;

namespace FFImageLoading.DataResolvers
{
    public class WrappedDataResolver : IDataResolver
    {
        readonly IDataResolver _resolver;
        //static readonly byte[]

        public WrappedDataResolver(IDataResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var resolved = await _resolver.Resolve(identifier, parameters, token);

            if (resolved.Item1 == null)
                throw new ArgumentNullException(nameof(parameters.Stream));

            if (!resolved.Item1.CanSeek)
            {
                using (resolved.Item1)
                {
                    var memoryStream = new MemoryStream();
                    await resolved.Item1.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    resolved = new Tuple<Stream, LoadingResult, ImageInformation>(memoryStream, resolved.Item2, resolved.Item3);
                }
            }

            if (resolved.Item3.Type == ImageInformation.ImageType.Unknown)
            {
                //READ HEADER
                const int headerLength = 4;
                byte[] header = new byte[headerLength];
                int offset = 0;
                while (offset < headerLength)
                {
                    int read = await resolved.Item1.ReadAsync(header, offset, headerLength - offset);
                    offset += read;
                }

                resolved.Item1.Position = 0;
                resolved.Item3.SetType(FileHeader.GetImageType(header));
            }

            return resolved;
        }
    }
}
