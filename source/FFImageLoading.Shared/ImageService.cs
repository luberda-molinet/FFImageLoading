using System.Collections.Generic;
using System;
using FFImageLoading.Config;
using FFImageLoading.Work;
using System.Net.Http;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Linq;


#if SILVERLIGHT
using FFImageLoading.Concurrency;
#else
using System.Collections.Concurrent;
#endif

namespace FFImageLoading
{
    public static class ImageService
    {
        private static volatile bool _initialized;
		private static object _initializeLock = new object();
		private static readonly MD5Helper _md5Helper = new MD5Helper();
		private static readonly ConcurrentDictionary<string, string> _fullKeyToKey = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Gets FFImageLoading configuration
        /// </summary>
        /// <value>The configuration used by FFImageLoading.</value>
        public static Configuration Config { get; private set; }

		public static void Initialize(Configuration configuration)
		{
			lock (_initializeLock)
			{
				_initialized = false;

				if (Config != null)
				{
					// If DownloadCache is not updated but HttpClient is then we inform DownloadCache
					if (configuration.HttpClient != null && configuration.DownloadCache == null)
					{
						configuration.DownloadCache = Config.DownloadCache;
						configuration.DownloadCache.DownloadHttpClient = configuration.HttpClient;
					}

					// Redefine these if they were provided only
					configuration.HttpClient = configuration.HttpClient ?? Config.HttpClient;
					configuration.Scheduler = configuration.Scheduler ?? Config.Scheduler;
					configuration.Logger = configuration.Logger ?? Config.Logger;
					configuration.DownloadCache = configuration.DownloadCache ?? Config.DownloadCache;
					configuration.LoadWithTransparencyChannel = configuration.LoadWithTransparencyChannel;
					configuration.FadeAnimationEnabled = configuration.FadeAnimationEnabled;
					configuration.TransformPlaceholders = configuration.TransformPlaceholders;
					configuration.DownsampleInterpolationMode = configuration.DownsampleInterpolationMode;

					// Skip configuration for maxCacheSize and diskCache. They cannot be redefined.
					if (configuration.Logger != null)
						configuration.Logger.Debug("Skip configuration for maxCacheSize and diskCache. They cannot be redefined.");
					configuration.MaxCacheSize = Config.MaxCacheSize;
					configuration.DiskCache = Config.DiskCache;
				}


				InitializeIfNeeded(configuration);
			}
		}

        /// <summary>
        /// Initialize ImageService default values. This can only be done once: during app start.
        /// </summary>
        /// <param name="maxCacheSize">Max cache size. If zero then 20% of the memory will be used.</param>
		/// <param name="httpClient">.NET HttpClient to use. If null then a.NET HttpClient is instanciated.</param>
        /// <param name="scheduler">Work scheduler used to organize/schedule loading tasks.</param>
        /// <param name="logger">Basic logger. If null a very simple implementation that prints to console is used.</param>
        /// <param name="diskCache">Disk cache. If null a default disk cache is instanciated that uses a journal mechanism.</param>
        /// <param name="downloadCache">Download cache. If null a default download cache is instanciated, which relies on the DiskCache</param>
		/// <param name="loadWithTransparencyChannel">Gets a value indicating whether images should be loaded with transparency channel. On Android we save 50% of the memory without transparency since we use 2 bytes per pixel instead of 4.</param>
		/// <param name="fadeAnimationEnabled">Defines if fading should be performed while loading images.</param>
        /// <param name="transformPlaceholders">Defines if transforms should be applied to placeholders.</param>
		/// <param name="downsampleInterpolationMode">Defines default downsample interpolation mode.</param>
		/// <param name="httpHeadersTimeout">Maximum time in seconds to wait to receive HTTP headers before the HTTP request is cancelled.</param>
		/// <param name="httpReadTimeout">Maximum time in seconds to wait before the HTTP request is cancelled.</param>
		[Obsolete("Use Initialize(Configuration configuration) overload")]
		public static void Initialize(int? maxCacheSize = null, HttpClient httpClient = null, IWorkScheduler scheduler = null, IMiniLogger logger = null,
			IDiskCache diskCache = null, IDownloadCache downloadCache = null, bool? loadWithTransparencyChannel = null, bool? fadeAnimationEnabled = null,
			bool? transformPlaceholders = null, InterpolationMode? downsampleInterpolationMode = null, int httpHeadersTimeout = 15, int httpReadTimeout = 30
		)
        {
			var cfg = new Configuration();

			if (httpClient != null) cfg.HttpClient = httpClient;
			if (scheduler != null) cfg.Scheduler = scheduler;
			if (logger != null) cfg.Logger = logger;
			if (diskCache != null) cfg.DiskCache = diskCache;
			if (downloadCache != null) cfg.DownloadCache = downloadCache;
			if (loadWithTransparencyChannel.HasValue) cfg.LoadWithTransparencyChannel = loadWithTransparencyChannel.Value;
			if (fadeAnimationEnabled.HasValue) cfg.FadeAnimationEnabled = fadeAnimationEnabled.Value;
			if (transformPlaceholders.HasValue) cfg.TransformPlaceholders = transformPlaceholders.Value;
			if (downsampleInterpolationMode.HasValue) cfg.DownsampleInterpolationMode = downsampleInterpolationMode.Value;
			cfg.HttpHeadersTimeout = httpHeadersTimeout;
			cfg.HttpReadTimeout = httpReadTimeout;
			if (maxCacheSize.HasValue) cfg.MaxCacheSize = maxCacheSize.Value;

			Initialize(cfg);
        }

		private static void InitializeIfNeeded(Configuration userDefinedConfig = null)
        {
			if (_initialized)
				return;

			lock (_initializeLock)
			{
				if (_initialized)
					return;

				if (userDefinedConfig == null)
					userDefinedConfig = new Configuration();

				var httpClient = userDefinedConfig.HttpClient ?? new HttpClient();

				if (userDefinedConfig.HttpReadTimeout > 0)
				{
					httpClient.Timeout = TimeSpan.FromSeconds(userDefinedConfig.HttpReadTimeout);
				}

				var logger = userDefinedConfig.Logger ?? new MiniLogger();
				var scheduler = userDefinedConfig.Scheduler ?? new WorkScheduler(logger);
				var diskCache = userDefinedConfig.DiskCache ?? DiskCache.CreateCache(typeof(ImageService).Name);
				var downloadCache = userDefinedConfig.DownloadCache ?? new DownloadCache(httpClient, diskCache);

				userDefinedConfig.HttpClient = httpClient;
				userDefinedConfig.Scheduler = scheduler;
				userDefinedConfig.Logger = logger;
				userDefinedConfig.DiskCache = diskCache;
				userDefinedConfig.DownloadCache = downloadCache;

				Config = userDefinedConfig;

				_initialized = true;
			}
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
		/// Constructs a new TaskParameter to load an image from a compiled drawable resource.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="resourceName">Name of the resource in drawable folder without extension</param>
		public static TaskParameter LoadCompiledResource(string resourceName)
		{
			InitializeIfNeeded();
			return TaskParameter.FromCompiledResource(resourceName);
		}

		public static TaskParameter LoadStream(Func<CancellationToken, Task<Stream>> stream)
		{
			InitializeIfNeeded();
			return TaskParameter.FromStream(stream);
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
			AddRequestToHistory(task);
        }

		/// <summary>
		/// Invalidates the memory cache.
		/// </summary>
		public static void InvalidateMemoryCache()
		{
			InitializeIfNeeded();
            ImageCache.Instance.Clear();
        }

		/// <summary>
		/// Invalidates the disk cache.
		/// </summary>
		public static void InvalidateDiskCache()
		{
			InitializeIfNeeded();
			Config.DiskCache.ClearAsync();
		}

		/// <summary>
		/// Invalidates the cache for given key.
		/// </summary>
		/// <param name="key">Concerns images with this key</param>
		/// <param name="cacheType">Memory cache, Disk cache or both</param>
		/// <param name="removeSimilar">If similar keys should be removed, ie: typically keys with extra transformations</param>
		public static void Invalidate(string key, CacheType cacheType, bool removeSimilar=false)
		{
			InitializeIfNeeded();

			if (cacheType == CacheType.All || cacheType == CacheType.Memory)
			{
				ImageCache.Instance.Remove(key);

				if (removeSimilar)
				{
					foreach (var otherKey in _fullKeyToKey.Where(pair => pair.Value == key).Select(pair => pair.Key))
					{
						ImageCache.Instance.Remove(otherKey);
					}
				}
			}

			if (cacheType == CacheType.All || cacheType == CacheType.Disk)
			{
				string hash = _md5Helper.MD5(key);
				Config.DiskCache.RemoveAsync(hash);
			}
		}

		private static void AddRequestToHistory(IImageLoaderTask task)
		{
			AddRequestToHistory(task.Parameters.Path, task.GetKey());
			AddRequestToHistory(task.Parameters.CustomCacheKey, task.GetKey());
			AddRequestToHistory(task.Parameters.LoadingPlaceholderPath, task.GetKey(task.Parameters.LoadingPlaceholderPath));
			AddRequestToHistory(task.Parameters.ErrorPlaceholderPath, task.GetKey(task.Parameters.ErrorPlaceholderPath));
		}

		private static void AddRequestToHistory(string baseKey, string fullKey)
		{
			if (string.IsNullOrWhiteSpace(baseKey) || string.IsNullOrWhiteSpace(fullKey))
				return;

			_fullKeyToKey.TryAdd(fullKey, baseKey);
		}
    }
}
