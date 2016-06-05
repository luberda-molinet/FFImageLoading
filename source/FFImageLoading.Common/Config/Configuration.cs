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
	public class Configuration
    {
		public Configuration()
		{
			// default values here:
			MaxMemoryCacheSize = 0; 
			LoadWithTransparencyChannel = false;
			FadeAnimationEnabled = true;
			FadeAnimationForCachedImages = true;
			FadeAnimationDuration = 500;
			TransformPlaceholders = true;
			DownsampleInterpolationMode = InterpolationMode.Default;
			HttpHeadersTimeout = 15;
			HttpReadTimeout = 30;
            VerbosePerformanceLogging = false;
            VerboseMemoryCacheLogging = false;
            VerboseLoadingCancelledLogging = false;
            VerboseLogging = false;
		}

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

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FFImageLoading.Config.Configuration"/> loads images with transparency channel. On Android we save 50% of the memory without transparency since we use 2 bytes per pixel instead of 4.
		/// </summary>
		/// <value><c>true</c> if FFIMageLoading loads images with transparency; otherwise, <c>false</c>.</value>
		public bool LoadWithTransparencyChannel { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FFImageLoading.Config.Configuration"/> fade animation enabled.
		/// </summary>
		/// <value><c>true</c> if fade animation enabled; otherwise, <c>false</c>.</value>
		public bool FadeAnimationEnabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating wheter fade animation for
		/// cached or local images should be enabled.
		/// </summary>
		/// <value><c>true</c> if fade animation for cached or local images; otherwise, <c>false</c>.</value>
		public bool FadeAnimationForCachedImages { get; set; }

		/// <summary>
		/// Gets or sets the default duration of the fade animation in ms.
		/// </summary>
		/// <value>The duration of the fade animation.</value>
		public int FadeAnimationDuration { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FFImageLoading.Config.Configuration"/> transforming place is enabled.
		/// </summary>
		/// <value><c>true</c> if transform should be applied to placeholder images; otherwise, <c>false</c>.</value>
		public bool TransformPlaceholders { get; set; }

		/// <summary>
		/// Gets or sets default downsample interpolation mode.
		/// </summary>
		/// <value>downsample interpolation mode</value>
		public InterpolationMode DownsampleInterpolationMode { get; set; }

		/// <summary>
		/// Gets or sets the maximum time in seconds to wait to receive HTTP headers before the HTTP request is cancelled.
		/// </summary>
		/// <value>The http connect timeout.</value>
		public int HttpHeadersTimeout { get; set; }

		/// <summary>
		/// Gets or sets the maximum time in seconds to wait before the HTTP request is cancelled.
		/// </summary>
		/// <value>The http read timeout.</value>
		public int HttpReadTimeout { get; set; }

		/// <summary>
		/// Gets or sets the maximum size of the memory cache in bytes.
		/// </summary>
		/// <value>The maximum size of the memory cache in bytes.</value>
		public int MaxMemoryCacheSize { get; set; }

		/// <summary>
		/// Milliseconds to wait prior to start any task.
		/// </summary>
		public int DelayInMs { get; set; }

        /// <summary>
        /// Enables / disables verbose performance logging.
        /// </summary>
        /// <value>The verbose performance logging.</value>
        public bool VerbosePerformanceLogging { get; set; }

        /// <summary>
        /// Enables / disables verbose memory cache logging.
        /// </summary>
        /// <value>The verbose memory cache logging.</value>
        public bool VerboseMemoryCacheLogging { get; set; }

        /// <summary>
        /// Enables / disables verbose image loading cancelled logging.
        /// </summary>
        /// <value>The verbose image loading cancelled logging.</value>
        public bool VerboseLoadingCancelledLogging { get; set; }

        /// <summary>
        /// Enables / disables  verbose logging. 
        /// IMPORTANT! If it's disabled are other verbose logging options are disabled too
        /// </summary>
        /// <value>The verbose logging.</value>
        public bool VerboseLogging { get; set; }
    }
}

