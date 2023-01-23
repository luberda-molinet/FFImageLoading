using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;
using FFImageLoading.Helpers;

namespace FFImageLoading.DataResolvers
{
    public class WrappedDataResolver : IDataResolver
    {
        private readonly IDataResolver _resolver;

        public WrappedDataResolver(IDataResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var resolved = await _resolver.Resolve(identifier, parameters, token).ConfigureAwait(false);

            if (resolved.Stream == null && resolved.Decoded == null)
                throw new ArgumentNullException($"{nameof(resolved.Stream)} and {nameof(resolved.Decoded)}");

			resolved.Stream = await resolved.Stream.AsSeekableStreamAsync().ConfigureAwait(false);

            if (resolved.Stream != null)
            {
                if (resolved.Stream.Length == 0)
                    throw new InvalidDataException("Zero length stream");

                if (resolved.Stream.Length < 32)
                    throw new InvalidDataException("Invalid stream");

                if (resolved.ImageInformation.Type == ImageInformation.ImageType.Unknown)
                {
                    //READ HEADER
                    const int headerLength = 4;
                    var header = new byte[headerLength];
                    var offset = 0;
                    while (offset < headerLength)
                    {
                        offset += await resolved.Stream.ReadAsync(header, offset, headerLength - offset).ConfigureAwait(false);
                    }

                    resolved.Stream.Position = 0;
                    resolved.ImageInformation.SetType(FileHeader.GetImageType(header));
                }

                if (resolved.ImageInformation.Type == ImageInformation.ImageType.JPEG)
                {
                    var exif = ExifHelper.Read(resolved.Stream);
                    resolved.Stream.Position = 0;
                    resolved.ImageInformation.SetExif(exif);
                }
            }

            return resolved;
        }
    }
}
