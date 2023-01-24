using System;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Config;
using FFImageLoading.Helpers;
using FFImageLoading.Work;

namespace FFImageLoading.Decoders
{
    public interface IDecoder<TDecoderContainer>
    {
        Task<IDecodedImage<TDecoderContainer>> DecodeAsync(Stream stream, string path, Work.ImageSource source, ImageInformation imageInformation, TaskParameter parameters);
    }
}
