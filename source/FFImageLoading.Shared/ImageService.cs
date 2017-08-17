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
using System.Net;
using System.Reflection;

#if SILVERLIGHT
using FFImageLoading.Concurrency;
#else
using System.Collections.Concurrent;
#endif

namespace FFImageLoading
{
    public class ImageService : IImageService
    {
        private volatile bool _initialized;
        private object _initializeLock = new object();
        private readonly MD5Helper _md5Helper = new MD5Helper();
        private Configuration _config;

        private static Lazy<ImageService> LazyInstance = new Lazy<ImageService>(() => new ImageService());
        public static IImageService Instance { get { return LazyInstance.Value; } }

        private ImageService() { }

        /// <summary>
        /// Gets FFImageLoading configuration
        /// </summary>
        /// <value>The configuration used by FFImageLoading.</value>
        public Configuration Config
        {
            get
            {
                InitializeIfNeeded();
                return _config;
            }

            set
            {
                _config = value;
            }
        }

        /// <summary>
        /// Initializes FFImageLoading with a default Configuration. 
        /// Also forces to run disk cache cleaning routines (avoiding delay for first image loading tasks)
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public void Initialize()
        {
            lock (_initializeLock)
            {
                _initialized = false;
                InitializeIfNeeded();
            }
        }

        /// <summary>
        /// Initializes FFImageLoading with a given Configuration. It allows to configure and override most of it.
        /// Also forces to run disk cache cleaning routines (avoiding delay for first image loading tasks)
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public void Initialize(Configuration configuration)
        {
            lock (_initializeLock)
            {
                _initialized = false;

                if (_config != null)
                {
                    // Redefine these if they were provided only
                    configuration.HttpClient = configuration.HttpClient ?? _config.HttpClient;
                    configuration.Scheduler = configuration.Scheduler ?? _config.Scheduler;
                    configuration.Logger = configuration.Logger ?? _config.Logger;
                    configuration.DownloadCache = configuration.DownloadCache ?? _config.DownloadCache;
                    configuration.DataResolverFactory = configuration.DataResolverFactory ?? _config.DataResolverFactory;
                    configuration.SchedulerMaxParallelTasksFactory = configuration.SchedulerMaxParallelTasksFactory ?? _config.SchedulerMaxParallelTasksFactory;
                    configuration.MD5Helper = configuration.MD5Helper ?? _config.MD5Helper;

                    // Skip configuration for maxMemoryCacheSize and diskCache. They cannot be redefined.
                    if (configuration.Logger != null)
                        configuration.Logger.Debug("Skip configuration for maxMemoryCacheSize and diskCache. They cannot be redefined.");
                    configuration.MaxMemoryCacheSize = _config.MaxMemoryCacheSize;
                    configuration.DiskCache = _config.DiskCache;
                }

                InitializeIfNeeded(configuration);
            }
        }

        private void InitializeIfNeeded(Configuration userDefinedConfig = null)
        {
            if (_initialized)
                return;

            lock (_initializeLock)
            {
                if (_initialized)
                    return;

                if (userDefinedConfig == null)
                    userDefinedConfig = new Configuration();

                var logger = new MiniLoggerWrapper(userDefinedConfig.Logger ?? new MiniLogger(), userDefinedConfig.VerboseLogging);
                userDefinedConfig.Logger = logger;

                var md5Helper = userDefinedConfig.MD5Helper ?? new MD5Helper();
                userDefinedConfig.MD5Helper = md5Helper;

                var httpClient = userDefinedConfig.HttpClient ?? new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });

                if (userDefinedConfig.HttpReadTimeout > 0)
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(userDefinedConfig.HttpReadTimeout);
                }
                userDefinedConfig.HttpClient = httpClient;

                var scheduler = userDefinedConfig.Scheduler ?? new WorkScheduler(userDefinedConfig, PlatformPerformance.Create());
                userDefinedConfig.Scheduler = scheduler;

                if (string.IsNullOrWhiteSpace(userDefinedConfig.DiskCachePath))
                {
                    var diskCache = userDefinedConfig.DiskCache ?? SimpleDiskCache.CreateCache("FFSimpleDiskCache", userDefinedConfig);
                    userDefinedConfig.DiskCache = diskCache;
                }
                else
                {
                    var diskCache = userDefinedConfig.DiskCache ?? new SimpleDiskCache(userDefinedConfig.DiskCachePath, userDefinedConfig);
                    userDefinedConfig.DiskCache = diskCache;
                }

                var downloadCache = userDefinedConfig.DownloadCache ?? new DownloadCache(userDefinedConfig);
                userDefinedConfig.DownloadCache = downloadCache;

                Config = userDefinedConfig;

                _initialized = true;
            }
        }

        private IWorkScheduler Scheduler
        {
            get
            {
                InitializeIfNeeded();
                return Config.Scheduler;
            }
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a file.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="filepath">Path to the file.</param>
        public TaskParameter LoadFile(string filepath)
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
        public TaskParameter LoadUrl(string url, TimeSpan? cacheDuration = null)
        {
            InitializeIfNeeded();
            return TaskParameter.FromUrl(url, cacheDuration);
        }

        /// <summary>
        /// Loads the string.
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="data">Data.</param>
        /// <param name="encoding">Encoding.</param>
        public TaskParameter LoadString(string data, DataEncodingType encoding = DataEncodingType.RAW)
        {
            InitializeIfNeeded();
            return TaskParameter.FromString(data, encoding);
        }

        /// <summary>
        /// Loads the base64 string.
        /// </summary>
        /// <returns>The base64 string.</returns>
        /// <param name="data">Data.</param>
        public TaskParameter LoadBase64String(string data)
        {
            return LoadString(data, DataEncodingType.Base64Encoded);
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a file from application bundle.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="filepath">Path to the file.</param>
        public TaskParameter LoadFileFromApplicationBundle(string filepath)
        {
            InitializeIfNeeded();
            return TaskParameter.FromApplicationBundle(filepath);
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a compiled drawable resource.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="resourceName">Name of the resource in drawable folder without extension</param>
        public TaskParameter LoadCompiledResource(string resourceName)
        {
            InitializeIfNeeded();
            return TaskParameter.FromCompiledResource(resourceName);
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a compiled drawable resource.
        /// eg. resource://YourProject.Resource.Resource.png
        /// eg. resource://YourProject.Resource.Resource.png?assembly=[FULL_ASSEMBLY_NAME]
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="resourceUri">Uri of the resource</param>
        public TaskParameter LoadEmbeddedResource(string resourceUri)
        {
            InitializeIfNeeded();
            return TaskParameter.FromEmbeddedResource(resourceUri);
        }

        public TaskParameter LoadEmbeddedResource(string resourceName, Assembly resourceAssembly)
        {
            InitializeIfNeeded();
            var uri = $"resource://{resourceName}?assembly={Uri.EscapeUriString(resourceAssembly.FullName)}";
            return TaskParameter.FromEmbeddedResource(uri);
        }

		/// <summary>
		/// Constructs a new TaskParameter to load an image from a Stream.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="resourceName">A function that allows a CancellationToken and returns the Stream to use. This function will be invoked by LoadStream().</param>
		public TaskParameter LoadStream(Func<CancellationToken, Task<Stream>> stream)
		{
			InitializeIfNeeded();
			return TaskParameter.FromStream(stream);
		}

        /// <summary>
        /// Gets a value indicating whether ImageService will exit tasks earlier
        /// </summary>
        /// <value><c>true</c> if it should exit tasks early; otherwise, <c>false</c>.</value>
        public bool ExitTasksEarly
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
        public void SetExitTasksEarly(bool exitTasksEarly)
        {
            Scheduler.SetExitTasksEarly(exitTasksEarly);
        }

        /// <summary>
        /// Sets a value indicating if all loading work should be paused (silently canceled).
        /// </summary>
        /// <param name="pauseWork">If set to <c>true</c> pause/cancel work.</param>
        public void SetPauseWork(bool pauseWork)
        {
            Scheduler.SetPauseWork(pauseWork);
        }

        /// <summary>
        /// Gets a value indicating whether this ImageService will pause tasks execution.
        /// </summary>
        /// <value><c>true</c> if pause work; otherwise, <c>false</c>.</value>
        public bool PauseWork
        {
            get
            {
                return Scheduler.PauseWork;
            }
        }

        /// <summary>
        /// Cancel any loading work for the given ImageView
        /// </summary>
        /// <param name="task">Image loading task to cancel.</param>
        public void CancelWorkFor(IImageLoaderTask task)
        {
            task?.Cancel();
        }

        /// <summary>
        /// Removes a pending image loading task from the work queue.
        /// </summary>
        /// <param name="task">Image loading task to remove.</param>
        public void RemovePendingTask(IImageLoaderTask task)
        {
            Scheduler.RemovePendingTask(task);
        }

        /// <summary>
        /// Queue an image loading task.
        /// </summary>
        /// <param name="task">Image loading task.</param>
        public void LoadImage(IImageLoaderTask task)
        {
			if (task == null)
				return;

			Scheduler.LoadImage(task);
        }

		/// <summary>
		/// Invalidates selected caches.
		/// </summary>
		/// <returns>An awaitable task.</returns>
		/// <param name="cacheType">Memory cache, Disk cache or both</param>
		public async Task InvalidateCacheAsync(CacheType cacheType)
		{
			InitializeIfNeeded();

			if (cacheType == CacheType.All || cacheType == CacheType.Memory)
			{
				InvalidateMemoryCache();
			}

			if (cacheType == CacheType.All || cacheType == CacheType.Disk)
			{
				await InvalidateDiskCacheAsync().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Invalidates the memory cache.
		/// </summary>
		public void InvalidateMemoryCache()
		{
			InitializeIfNeeded();
            ImageCache.Instance.Clear();
        }

		/// <summary>
		/// Invalidates the disk cache.
		/// </summary>
		public Task InvalidateDiskCacheAsync()
		{
			InitializeIfNeeded();
			return Config.DiskCache.ClearAsync();
		}

		/// <summary>
		/// Invalidates the cache for given key.
		/// </summary>
		/// <returns>The async.</returns>
		/// <param name="key">Concerns images with this key.</param>
		/// <param name="cacheType">Memory cache, Disk cache or both</param>
		/// <param name="removeSimilar">If similar keys should be removed, ie: typically keys with extra transformations</param>
		public async Task InvalidateCacheEntryAsync(string key, CacheType cacheType, bool removeSimilar=false)
		{
			InitializeIfNeeded();

			if (cacheType == CacheType.All || cacheType == CacheType.Memory)
			{
				ImageCache.Instance.Remove(key);

				if (removeSimilar)
				{
					ImageCache.Instance.RemoveSimilar(key);
				}
			}

			if (cacheType == CacheType.All || cacheType == CacheType.Disk)
			{
				string hash = _md5Helper.MD5(key);
				await Config.DiskCache.RemoveAsync(hash).ConfigureAwait(false);
			}
		}

        /// <summary>
        /// Cancels tasks that match predicate.
        /// </summary>
        /// <param name="predicate">Predicate for finding relevant tasks to cancel.</param>
        public void Cancel(Func<IImageLoaderTask, bool> predicate)
        {
            Scheduler.Cancel(predicate);
        }

        /// <summary>
        /// Cancels tasks that match predicate.
        /// </summary>
        /// <param name="predicate">Predicate for finding relevant tasks to cancel.</param>
        public void Cancel(Func<TaskParameter, bool> predicate)
        {
            Scheduler.Cancel(task => task.Parameters != null && predicate(task.Parameters));
        }
    }
}
