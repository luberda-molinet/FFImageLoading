using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.Work;
using FFImageLoading.Helpers;
using System.Net.Http;
using System.Net;

namespace FFImageLoading
{
    public abstract class ImageServiceBase<TImageContainer> : IImageService
    {
        protected bool _initialized;
        protected bool _isInitializing;
        protected object _initializeLock = new object();

        protected virtual void PlatformSpecificConfiguration(Configuration configuration) { }

        protected abstract IMD5Helper CreatePlatformMD5HelperInstance(Configuration configuration);
        protected abstract IMiniLogger CreatePlatformLoggerInstance(Configuration configuration);
        protected abstract IDiskCache CreatePlatformDiskCacheInstance(Configuration configuration);
        protected abstract IPlatformPerformance CreatePlatformPerformanceInstance(Configuration configuration);
        protected abstract IMainThreadDispatcher CreateMainThreadDispatcherInstance(Configuration configuration);
        protected abstract IDataResolverFactory CreateDataResolverFactoryInstance(Configuration configuration);
        protected abstract void SetTaskForTarget(IImageLoaderTask currentTask);
        public abstract void CancelWorkForView(object view);

        public abstract int DpToPixels(double dp);
        public abstract double PixelsToDp(double px);

        Configuration _config;
        public Configuration Config
        {
            get
            {
                InitializeIfNeeded();
                return _config;
            }
        }

        public bool ExitTasksEarly => Scheduler.ExitTasksEarly;
        public bool PauseWork => Scheduler.PauseWork;

        protected IDiskCache DiskCache => Config.DiskCache;
        protected IWorkScheduler Scheduler => Config.Scheduler;
        protected IMD5Helper MD5Helper => Config.MD5Helper;
        protected abstract IMemoryCache<TImageContainer> MemoryCache { get; }

        public void Initialize()
        {
            if (_isInitializing)
                return;

            lock (_initializeLock)
            {
                _initialized = false;
                InitializeIfNeeded();
            }
        }

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
                    configuration.MainThreadDispatcher = configuration.MainThreadDispatcher ?? _config.MainThreadDispatcher;
                    configuration.DataResolverFactory = configuration.DataResolverFactory ?? _config.DataResolverFactory;

                    // Skip configuration for maxMemoryCacheSize and diskCache. They cannot be redefined.
                    if (configuration.DiskCache == null)
                    {
                        configuration.MaxMemoryCacheSize = _config.MaxMemoryCacheSize;
                        configuration.DiskCache = _config.DiskCache;
                    }
                    else
                    {
                        configuration.Logger.Debug("Skipping configuration for maxMemoryCacheSize and diskCache. They cannot be redefined.");
                    }
                }

                InitializeIfNeeded(configuration);
            }
        }

        void InitializeIfNeeded(Configuration userDefinedConfig = null)
        {
            if (_initialized && userDefinedConfig == null)
                return;

            lock (_initializeLock)
            {
                if (_isInitializing || (_initialized && userDefinedConfig == null))
                    return;

                _isInitializing = true;

                if (userDefinedConfig == null)
                {
                    userDefinedConfig = new Configuration();
                    PlatformSpecificConfiguration(userDefinedConfig);
                }

                _config = userDefinedConfig;

                var httpClient = userDefinedConfig.HttpClient ?? new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
                if (userDefinedConfig.HttpReadTimeout > 0)
                {
                    try
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(userDefinedConfig.HttpReadTimeout);
                    }
                    catch (Exception)
                    {
                    }
                }
                
				if (StaticLocks.DecodingLock == null)
					StaticLocks.DecodingLock = new SemaphoreSlim(userDefinedConfig.DecodingMaxParallelTasks, userDefinedConfig.DecodingMaxParallelTasks);

				if (userDefinedConfig.Logger == null || !(userDefinedConfig.Logger is MiniLoggerWrapper))
                    userDefinedConfig.Logger = new MiniLoggerWrapper(userDefinedConfig.Logger ?? CreatePlatformLoggerInstance(userDefinedConfig), userDefinedConfig.VerboseLogging);

                userDefinedConfig.MD5Helper = userDefinedConfig.MD5Helper ?? CreatePlatformMD5HelperInstance(userDefinedConfig);
                userDefinedConfig.HttpClient = httpClient;
                userDefinedConfig.Scheduler = userDefinedConfig.Scheduler ?? new WorkScheduler(userDefinedConfig, (userDefinedConfig.VerbosePerformanceLogging ? CreatePlatformPerformanceInstance(userDefinedConfig) : new EmptyPlatformPerformance()));
                userDefinedConfig.DiskCache = userDefinedConfig.DiskCache ?? CreatePlatformDiskCacheInstance(userDefinedConfig);
                userDefinedConfig.DownloadCache = userDefinedConfig.DownloadCache ?? new DownloadCache(userDefinedConfig);
                userDefinedConfig.MainThreadDispatcher = userDefinedConfig.MainThreadDispatcher ?? CreateMainThreadDispatcherInstance(userDefinedConfig);
                userDefinedConfig.DataResolverFactory = userDefinedConfig.DataResolverFactory ?? CreateDataResolverFactoryInstance(userDefinedConfig);

                _initialized = true;
                _isInitializing = false;
            }
        }

        public TaskParameter LoadFile(string filepath)
        {
            return TaskParameter.FromFile(filepath);
        }

        public TaskParameter LoadUrl(string url, TimeSpan? cacheDuration = null)
        {
            return TaskParameter.FromUrl(url, cacheDuration);
        }

        public TaskParameter LoadString(string data, DataEncodingType encoding = DataEncodingType.RAW)
        {
            return TaskParameter.FromString(data, encoding);
        }

        public TaskParameter LoadBase64String(string data)
        {
            return LoadString(data, DataEncodingType.Base64Encoded);
        }

        public TaskParameter LoadFileFromApplicationBundle(string filepath)
        {
            return TaskParameter.FromApplicationBundle(filepath);
        }

        public TaskParameter LoadCompiledResource(string resourceName)
        {
            return TaskParameter.FromCompiledResource(resourceName);
        }

        public TaskParameter LoadEmbeddedResource(string resourceUri)
        {
            return TaskParameter.FromEmbeddedResource(resourceUri);
        }

        public TaskParameter LoadEmbeddedResource(string resourceName, Assembly resourceAssembly)
        {
            var uri = $"resource://{resourceName}?assembly={Uri.EscapeUriString(resourceAssembly.FullName)}";
            return TaskParameter.FromEmbeddedResource(uri);
        }

        public TaskParameter LoadStream(Func<CancellationToken, Task<Stream>> stream)
        {
            return TaskParameter.FromStream(stream);
        }

		[Obsolete("Use SetPauseWork(bool pauseWork, bool cancelExistingTasks = false)")]
		public void SetExitTasksEarly(bool exitTasksEarly)
        {
            Scheduler.SetExitTasksEarly(exitTasksEarly);
        }

		public void SetPauseWorkAndCancelExisting(bool pauseWork) => SetPauseWork(pauseWork, true);

		public void SetPauseWork(bool pauseWork, bool cancelExisting = false)
        {
            Scheduler.SetPauseWork(pauseWork, cancelExisting);
        }

        public void CancelWorkFor(IImageLoaderTask task)
        {
            task?.Cancel();
        }

        public void RemovePendingTask(IImageLoaderTask task)
        {
            Scheduler.RemovePendingTask(task);
        }

        public void LoadImage(IImageLoaderTask task)
        {
            if (task == null)
                return;

            if (!task.Parameters.Preload)
                SetTaskForTarget(task);
            
            Scheduler.LoadImage(task);
        }

        public async Task InvalidateCacheAsync(CacheType cacheType)
        {
            if (cacheType == CacheType.All || cacheType == CacheType.Memory)
            {
                InvalidateMemoryCache();
            }

            if (cacheType == CacheType.All || cacheType == CacheType.Disk)
            {
                await InvalidateDiskCacheAsync().ConfigureAwait(false);
            }
        }

        public void InvalidateMemoryCache()
        {
            MemoryCache.Clear();
        }

        public Task InvalidateDiskCacheAsync()
        {
            return DiskCache.ClearAsync();
        }

        public async Task InvalidateCacheEntryAsync(string key, CacheType cacheType, bool removeSimilar = false)
        {
            if (cacheType == CacheType.All || cacheType == CacheType.Memory)
            {
                MemoryCache.Remove(key);

                if (removeSimilar)
                {
                    MemoryCache.RemoveSimilar(key);
                }
            }

            if (cacheType == CacheType.All || cacheType == CacheType.Disk)
            {
                string hash = MD5Helper.MD5(key);
                await DiskCache.RemoveAsync(hash).ConfigureAwait(false);
            }
        }

        public void Cancel(Func<IImageLoaderTask, bool> predicate)
        {
            Scheduler.Cancel(predicate);
        }

        public void Cancel(Func<TaskParameter, bool> predicate)
        {
            Scheduler.Cancel(task => task.Parameters != null && predicate(task.Parameters));
        }
    }
}
