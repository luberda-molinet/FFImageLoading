using System;
using System.IO;

namespace FFImageLoading.Helpers
{
    public interface IImageFileDecoder<INativeImageContainer>
    {
        INativeImageContainer Decode(Stream stream);
    }
}
