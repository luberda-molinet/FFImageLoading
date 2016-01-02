using FFImageLoading.Cache;
using FFImageLoading.Work;
using System;

namespace FFImageLoading.DataResolver
{
    public static class DataResolverFactory
    {
        public static IDataResolver GetResolver(ImageSource source, TaskParameter parameter, IDownloadCache downloadCache)
        {
            switch (source)
            {
                case ImageSource.ApplicationBundle:
                case ImageSource.CompiledResource:
                    return new ResourceDataResolver(source);
                case ImageSource.Filepath:
                    return new FilePathDataResolver(source);
                case ImageSource.Url:
                    return new UrlDataResolver(parameter, downloadCache);
                default:
                    throw new ArgumentException("Unknown type of ImageSource");
            }
        }
    }
}
