#if !ANDROID && !WINDOWS && !IOS && !TIZEN && !MACCATALYST
using System;
using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.DataResolvers;
using FFImageLoading.Work;

namespace FFImageLoading
{
    public class MockDataResolverFactory : IDataResolverFactory
    {
		public MockDataResolverFactory(
            IConfiguration configuration,
            IDownloadCache downloadCache)
		{
            Configuration = configuration;
            DownloadCache = downloadCache;
		}

		protected readonly IConfiguration Configuration;
		protected readonly IDownloadCache DownloadCache;

		public IDataResolver GetResolver(string identifier, Work.ImageSource source, TaskParameter parameters)
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
                    return new UrlDataResolver(Configuration, DownloadCache);
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
