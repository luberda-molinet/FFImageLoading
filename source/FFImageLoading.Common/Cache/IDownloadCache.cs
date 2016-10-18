using System;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Threading;
using FFImageLoading.Work;
using FFImageLoading.Config;

namespace FFImageLoading.Cache
{
	public interface IDownloadCache
	{
        Task<CacheStream> DownloadAndCacheIfNeededAsync (string url, TaskParameter parameters, Configuration configuration, CancellationToken token);
	}
}

