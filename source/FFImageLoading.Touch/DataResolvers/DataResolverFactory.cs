using System;
using FFImageLoading.Work;
using FFImageLoading.Config;

namespace FFImageLoading.DataResolvers
{
    public class DataResolverFactory : IDataResolverFactory
    {
        public IDataResolver GetResolver(string identifier, ImageSource source, TaskParameter parameters, Configuration configuration)
        {
            switch (source)
            {
                case ImageSource.ApplicationBundle:
                case ImageSource.Filepath:
                    return new FileDataResolver(source);
                case ImageSource.CompiledResource:
                    return new BundleDataResolver();
                case ImageSource.Url:
                    return new UrlDataResolver(parameters, configuration);
                default:
                    throw new ArgumentException("Unknown type of ImageSource");
            }
        }
    }
}

