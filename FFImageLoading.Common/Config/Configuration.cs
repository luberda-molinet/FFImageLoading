using System;
using System.Net.Http;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using FFImageLoading.Work;

namespace FFImageLoading.Config
{
    public class Configuration
    {
        public Configuration(HttpClient httpClient = null, IWorkScheduler scheduler = null, IMiniLogger logger = null,
            IDiskCache diskCache = null, IDownloadCache downloadCache = null)
        {
            HttpClient = httpClient;
            Scheduler = scheduler;
            Logger = logger;
            DiskCache = diskCache;
            DownloadCache = downloadCache;
        }

        public HttpClient HttpClient { get; private set; }

        public IWorkScheduler Scheduler { get; private set; }

        public IMiniLogger Logger { get; private set; }

        public IDiskCache DiskCache { get; private set; }

        public IDownloadCache DownloadCache { get; private set; }
    }
}

