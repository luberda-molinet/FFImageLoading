using System;
using FFImageLoading.Work;
using FFImageLoading.Config;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;

namespace FFImageLoading.DataResolvers
{
    public class DataResolverFactory : IDataResolverFactory
    {
		public DataResolverFactory(IMainThreadDispatcher mainThreadDispatcher, IConfiguration configuration, IDownloadCache downloadCache) {

			this.mainThreadDispatcher= mainThreadDispatcher;
			this.configuration= configuration;
			this.downloadCache= downloadCache;
		}

		readonly IMainThreadDispatcher mainThreadDispatcher;
		readonly IConfiguration configuration;
		readonly IDownloadCache downloadCache;

		public virtual IDataResolver GetResolver(string identifier, Work.ImageSource source, TaskParameter parameters)
        {
            switch (source)
            {
                case Work.ImageSource.Filepath:
                    return new FileDataResolver();
				case Work.ImageSource.ApplicationBundle:
				case Work.ImageSource.CompiledResource:
                    return new BundleDataResolver(mainThreadDispatcher);
                case Work.ImageSource.Url:
                    if (!string.IsNullOrWhiteSpace(identifier) && identifier.IsDataUrl())
                        return new DataUrlResolver();
                    return new UrlDataResolver(configuration, downloadCache);
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

