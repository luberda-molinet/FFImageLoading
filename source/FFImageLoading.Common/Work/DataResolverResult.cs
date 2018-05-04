using System;
using System.IO;

namespace FFImageLoading.Work
{
    public class DataResolverResult<TImageContainer> : DataResolverResult
    {
        public DataResolverResult(TImageContainer imageContainer, LoadingResult loadingResult, ImageInformation imageInformation)
        {
            ImageContainer = imageContainer;
            LoadingResult = loadingResult;
            ImageInformation = imageInformation;
        }
    }

    public class DataResolverResult
    {
        public DataResolverResult()
        {
        }

        public DataResolverResult(Stream stream, LoadingResult loadingResult, ImageInformation imageInformation)
        {
            Stream = stream;
            LoadingResult = loadingResult;
            ImageInformation = imageInformation;
        }

        public Stream Stream { get; internal set; }

        public object ImageContainer { get; internal set; }

        public LoadingResult LoadingResult { get; internal set; }

        public ImageInformation ImageInformation { get; internal set; }
    }
}
