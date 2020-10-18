using System;
using System.IO;

namespace FFImageLoading.Work
{
    public class DataResolverResult
    {
        public DataResolverResult()
        {         
        }

        public DataResolverResult(IDecodedImage<object> decoded, LoadingResult loadingResult, ImageInformation imageInformation)
        {
            Decoded = decoded;
            LoadingResult = loadingResult;
            ImageInformation = imageInformation;
        }

        public DataResolverResult(Stream stream, LoadingResult loadingResult, ImageInformation imageInformation)
        {
            Stream = stream;
            LoadingResult = loadingResult;
            ImageInformation = imageInformation;
        }

        public Stream Stream { get; internal set; }

        public IDecodedImage<object> Decoded { get; internal set; }

        public LoadingResult LoadingResult { get; internal set; }

        public ImageInformation ImageInformation { get; internal set; }
    }
}
