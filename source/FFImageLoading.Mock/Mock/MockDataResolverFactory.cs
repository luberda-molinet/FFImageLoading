using System;
using FFImageLoading.Config;
using FFImageLoading.DataResolvers;
using FFImageLoading.Work;

namespace FFImageLoading.Mock
{
    public class MockDataResolverFactory : IDataResolverFactory
    {
        public IDataResolver GetResolver(string identifier, ImageSource source, TaskParameter parameters, Configuration configuration)
        {
            switch (source)
            {
                case ImageSource.ApplicationBundle:
                    throw new NotImplementedException();
                case ImageSource.CompiledResource:
                    throw new NotImplementedException();
                case ImageSource.Filepath:
                    throw new NotImplementedException();
                case ImageSource.Url:
                    if (!string.IsNullOrWhiteSpace(identifier) && identifier.IsDataUrl())
                        return new DataUrlResolver();
                    return new UrlDataResolver(configuration);
                case ImageSource.Stream:
                    return new StreamDataResolver();
                case ImageSource.EmbeddedResource:
                    return new EmbeddedResourceResolver();
                default:
                    throw new NotSupportedException("Unknown type of ImageSource");
            }
        }
    }
}
