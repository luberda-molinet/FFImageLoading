#if !ANDROID && !WINDOWS && !IOS && !TIZEN && !MACCATALYST
using System;
using FFImageLoading.Config;
using FFImageLoading.DataResolvers;
using FFImageLoading.Work;

namespace FFImageLoading
{
    public class DataResolverFactory : IDataResolverFactory
    {
        public IDataResolver GetResolver(string identifier, Work.ImageSource source, TaskParameter parameters, Configuration configuration)
        {
            switch (source)
            {
                case Work.ImageSource.ApplicationBundle:
                    throw new NotImplementedException();
                case Work.ImageSource.CompiledResource:
                    throw new NotImplementedException();
                case Work.ImageSource.Filepath:
                    throw new NotImplementedException();
                case Work.ImageSource.Url:
                    if (!string.IsNullOrWhiteSpace(identifier) && identifier.IsDataUrl())
                        return new DataUrlResolver();
                    return new UrlDataResolver(configuration);
                case Work.ImageSource.Stream:
                    return new StreamDataResolver();
                case Work.ImageSource.EmbeddedResource:
                    return new EmbeddedResourceResolver();
                default:
                    throw new NotSupportedException("Unknown type of ImageSource");
            }
        }
    }
}
#endif
