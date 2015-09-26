using System;
using FFImageLoading.Work;
using FFImageLoading.Cache;

namespace FFImageLoading.Work.StreamResolver
{
	public static class StreamResolverFactory
	{

		public static IStreamResolver GetResolver(ImageSource source, TaskParameter parameter, IDownloadCache downloadCache)
		{
			switch (source)
			{
				case ImageSource.ApplicationBundle:
					return new ApplicationBundleStreamResolver();
				case ImageSource.CompiledResource:
					return new CompiledResourceStreamResolver();
				case ImageSource.Filepath:
					return new FilePathStreamResolver();
				case ImageSource.Url:
					return new UrlStreamResolver(parameter, downloadCache);
				default:
					throw new ArgumentException("Unknown type of ImageSource");
			}
		}

	}
}

