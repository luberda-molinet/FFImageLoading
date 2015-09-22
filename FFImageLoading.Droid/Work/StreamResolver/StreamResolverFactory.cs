using System;
using FFImageLoading.Work;
using Java.Lang;
using FFImageLoading.Cache;

namespace FFImageLoading
{
	public class StreamResolverFactory
	{

		public static IStreamResolver GetResolver(ImageSource source, TaskParameter parameter, IDownloadCache downloadCache) {
			switch (source)
			{
				case ImageSource.ApplicationBundle:
					return new ApplicationBundleStreamResolver();
				case ImageSource.Filepath:
					return new FilePathStreamResolver();
				case ImageSource.Url:
					return new UrlStreamResolver(parameter, downloadCache);
				default:
					throw new IllegalArgumentException("Unknown type of ImageSource");
			}
		}

	}
}

