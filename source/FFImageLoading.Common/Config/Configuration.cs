using System;
using System.Net.Http;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using FFImageLoading.Work;

namespace FFImageLoading.Config
{
	/// <summary>
	/// Configuration.
	/// </summary>
	public class Configuration : MultiplatformConfiguration
    {
		/// <summary>
		/// Gets or sets the http client used for web requests.
		/// </summary>
		/// <value>The http client used for web requests.</value>
        public HttpClient HttpClient { get; set; }

        /// <summary>
		/// Gets or sets the scheduler used to organize/schedule image loading tasks.
        /// </summary>
        /// <value>The scheduler used to organize/schedule image loading tasks.</value>
        public IWorkScheduler Scheduler { get; set; }

        /// <summary>
		/// Gets or sets the logger used to receive debug/error messages.
        /// </summary>
        /// <value>The logger.</value>
        public IMiniLogger Logger { get; set; }

        /// <summary>
		/// Gets or sets the disk cache.
        /// </summary>
        /// <value>The disk cache.</value>
        public IDiskCache DiskCache { get; set; }

        /// <summary>
		/// Gets or sets the download cache. Download cache is responsible for retrieving data from the web, or taking from the disk cache.
        /// </summary>
        /// <value>The download cache.</value>
        public IDownloadCache DownloadCache { get; set; }
    }
}

