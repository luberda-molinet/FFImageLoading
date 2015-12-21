using System;
using FFImageLoading.Work;
using FFImageLoading.Cache;

namespace FFImageLoading.Work.DataResolver
{
	public static class DataResolverFactory
	{

		public static IDataResolver GetResolver(ImageSource source, TaskParameter parameter, IDownloadCache downloadCache)
		{
			switch (source)
			{
				case ImageSource.ApplicationBundle:
				case ImageSource.Filepath:
					return new FilePathDataResolver(source);
				case ImageSource.CompiledResource:
					return new AssetCatalogDataResolver();
				case ImageSource.Url:
					return new UrlDataResolver(parameter, downloadCache);
				default:
					throw new ArgumentException("Unknown type of ImageSource");
			}
		}

	}
}

