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

        public WrappedDataResolver(IDataResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var resolved = await _resolver.Resolve(identifier, parameters, token);

            if (resolved.Stream == null && resolved.Decoded == null)
                throw new ArgumentNullException($"{nameof(resolved.Stream)} and {nameof(resolved.Decoded)}");

            if (resolved.Stream != null && !resolved.Stream.CanSeek)
            {
                using (resolved.Stream)
                {
                    var memoryStream = new MemoryStream();
                    await resolved.Stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    resolved = new DataResolverResult(memoryStream, resolved.LoadingResult, resolved.ImageInformation);
                }
            }

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
                    byte[] header = new byte[headerLength];
                    int offset = 0;
                    while (offset < headerLength)
                    {
                        int read = await resolved.Stream.ReadAsync(header, offset, headerLength - offset);
                        offset += read;
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
