using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System;
using FFImageLoading.Config;
using FFImageLoading.Work;
using System.Net.Http;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using FFImageLoading.Extensions;
using System.Threading;

namespace FFImageLoading
{
    public static class ImageService
    {
        private static bool _initialized;

        /// <summary>
        /// Gets FFImageLoading configuration
        /// </summary>
        /// <value>The configuration used by FFImageLoading.</value>
        public static Configuration Config { get; private set; }

        /// <summary>
        /// Initialize ImageService default values. This can only be done once: during app start.
        /// </summary>
        /// <param name="maxCacheSize">Max cache size. If zero then 20% of the memory will be used.</param>
        /// <param name="httpClient">.NET HttpClient to use. If null then a ModernHttpClient is instanciated.</param>
        /// <param name="scheduler">Work scheduler used to organize/schedule loading tasks.</param>
        /// <param name="logger">Basic logger. If null a very simple implementation that prints to console is used.</param>
        /// <param name="diskCache">Disk cache. If null a default disk cache is instanciated that uses a journal mechanism.</param>
        /// <param name="downloadCache">Download cache. If null a default download cache is instanciated, which relies on the DiskCache</param>
        public static void Initialize(int maxCacheSize = 0, HttpClient httpClient = null, IWorkScheduler scheduler = null, IMiniLogger logger = null,
            IDiskCache diskCache = null, IDownloadCache downloadCache = null)
        {
            if (_initialized)
                throw new Exception("FFImageLoading.ImageService is already initialized");

            InitializeIfNeeded();
        }

        private static void InitializeIfNeeded(int maxCacheSize = 0, HttpClient httpClient = null, IWorkScheduler scheduler = null, IMiniLogger logger = null,
            IDiskCache diskCache = null, IDownloadCache downloadCache = null)
        {
            if (_initialized)
                return;

            var userDefinedConfig = new Configuration(maxCacheSize, httpClient, scheduler, logger, diskCache, downloadCache);
            Config = GetDefaultConfiguration(userDefinedConfig);

            _initialized = true;
        }

        private static Configuration GetDefaultConfiguration(Configuration userDefinedConfig)
        {
            var httpClient = userDefinedConfig.HttpClient ?? new HttpClient(new ModernHttpClient.NativeMessageHandler());

            var logger = userDefinedConfig.Logger ?? new MiniLogger();
            var scheduler = userDefinedConfig.Scheduler ?? new WorkScheduler(logger);
            var diskCache = userDefinedConfig.DiskCache ?? DiskCache.CreateCache(typeof(ImageService).Name);
            var downloadCache = userDefinedConfig.DownloadCache ?? new DownloadCache(httpClient, diskCache);

            return new Configuration(
                userDefinedConfig.MaxCacheSize,
                httpClient,
                scheduler,
                logger,
                diskCache,
                downloadCache
            );
        }

        private static IWorkScheduler Scheduler
        {
            get {
                InitializeIfNeeded();
                return Config.Scheduler;
            }
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a file.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="filepath">Path to the file.</param>
        public static TaskParameter LoadFile(string filepath)
        {
            InitializeIfNeeded();
            return TaskParameter.FromFile(filepath);
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a URL.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="url">URL to the file</param>
        /// <param name="cacheDuration">How long the file will be cached on disk</param>
        public static TaskParameter LoadUrl(string url, TimeSpan? cacheDuration = null)
        {
            InitializeIfNeeded();
            return TaskParameter.FromUrl(url, cacheDuration);
        }

		/// <summary>
		/// Constructs a new TaskParameter to load an image from a file from application bundle.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="filepath">Path to the file.</param>
		public static TaskParameter LoadFileFromApplicationBundle(string filepath)
		{
			InitializeIfNeeded();
			return TaskParameter.FromApplicationBundle(filepath);
		}

        /// <summary>
        /// Gets a value indicating whether ImageService will exit tasks earlier
        /// </summary>
        /// <value><c>true</c> if it should exit tasks early; otherwise, <c>false</c>.</value>
        public static bool ExitTasksEarly
        {
            get
            {
                return Scheduler.ExitTasksEarly;
            }
        }

        /// <summary>
        /// Sets a value indicating whether ImageService will exit tasks earlier
        /// </summary>
        /// <param name="exitTasksEarly">If set to <c>true</c> exit tasks early.</param>
        public static void SetExitTasksEarly(bool exitTasksEarly)
        {
            Scheduler.SetExitTasksEarly(exitTasksEarly);
        }

        /// <summary>
        /// Sets a value indicating if all loading work should be paused (silently canceled).
        /// </summary>
        /// <param name="pauseWork">If set to <c>true</c> pause/cancel work.</param>
        public static void SetPauseWork(bool pauseWork)
        {
            Scheduler.SetPauseWork(pauseWork);
        }

        /// <summary>
        /// Cancel any loading work for the given ImageView
        /// </summary>
        /// <param name="task">Image loading task to cancel.</param>
        public static void CancelWorkFor(IImageLoaderTask task)
        {
            Scheduler.Cancel(task);
        }

        /// <summary>
        /// Removes a pending image loading task from the work queue.
        /// </summary>
        /// <param name="task">Image loading task to remove.</param>
        public static void RemovePendingTask(IImageLoaderTask task)
        {
            Scheduler.RemovePendingTask(task);
        }

        /// <summary>
        /// Queue an image loading task.
        /// </summary>
        /// <param name="task">Image loading task.</param>
        public static void LoadImage(IImageLoaderTask task)
        {
            Scheduler.LoadImage(task);
        }
    }
}