using System;
using FFImageLoading.Work;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;

namespace FFImageLoading.Work.DataResolver
{
	public static class DataResolverFactory
	{

		public static IDataResolver GetResolver(ImageSource source, TaskParameter parameter, IDownloadCache downloadCache, IMainThreadDispatcher mainThreadDispatcher)
		{
			switch (source)
			{
				case ImageSource.ApplicationBundle:
				case ImageSource.Filepath:
					return new FilePathDataResolver(source);
				case ImageSource.CompiledResource:
					return new AssetCatalogDataResolver(mainThreadDispatcher);
				case ImageSource.Url:
					return new UrlDataResolver(parameter, downloadCache);
				default:
					throw new ArgumentException("Unknown type of ImageSource");
			}
		}

	}
}

