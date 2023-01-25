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
	public abstract class ImageServiceBase<TImageContainer> : IImageService<TImageContainer>
    {
		public ImageServiceBase(
			IConfiguration configuration,
			IMD5Helper mD5Helper,
			IMiniLogger miniLogger,
			IPlatformPerformance platformPerformance,
			IMainThreadDispatcher mainThreadDispatcher,
			IDataResolverFactory dataResolverFactory,
			IDownloadCache downloadCache,
            IWorkScheduler workScheduler)
		{
			_config = configuration;
			Md5Helper = mD5Helper;
			Logger = miniLogger;
			PlatformPerformance = platformPerformance;
			Dispatcher = mainThreadDispatcher;
			DataResolverFactory = dataResolverFactory;
			DownloadCache = downloadCache;
            Scheduler = workScheduler;
		}

		public IMD5Helper Md5Helper { get; }
		public IMiniLogger Logger { get; }
		public IPlatformPerformance PlatformPerformance { get; }
		public IMainThreadDispatcher Dispatcher { get; }
		public IDataResolverFactory DataResolverFactory { get; }
		public IDiskCache DiskCache { get; }
		public IWorkScheduler Scheduler { get; }
		public IMD5Helper MD5Helper { get; }

		public IDownloadCache DownloadCache { get; }

		public abstract IMemoryCache<TImageContainer> MemoryCache { get; }

		protected bool _initialized;
        protected bool _isInitializing;
        protected object _initializeLock = new object();

        protected virtual void PlatformSpecificConfiguration(IConfiguration configuration) { }

        protected abstract void SetTaskForTarget(IImageLoaderTask currentTask);
        public abstract void CancelWorkForView(object view);

		public abstract IImageLoaderTask CreateTask(TaskParameter parameters);

		public abstract IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<TImageContainer, TImageView> target) where TImageView : class;




		public abstract int DpToPixels(double dp, double scale);
        public abstract double PixelsToDp(double px, double scale);

        IConfiguration _config;
        public IConfiguration Configuration
        {
            get
            {
                InitializeIfNeeded();
                return _config;
            }
        }

        public bool ExitTasksEarly => Scheduler.ExitTasksEarly;
        public bool PauseWork => Scheduler.PauseWork;


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

        public void Initialize(IConfiguration configuration)
        {
            lock (_initializeLock)
            {
                _initialized = false;

                if (_config != null)
                {
                    // Redefine these if they were provided only
                    configuration.HttpClient = configuration.HttpClient ?? _config.HttpClient;
                    configuration.SchedulerMaxParallelTasksFactory = configuration.SchedulerMaxParallelTasksFactory ?? _config.SchedulerMaxParallelTasksFactory;
                    
                    configuration.MaxMemoryCacheSize = _config.MaxMemoryCacheSize;
                }

                InitializeIfNeeded(configuration);
            }
        }

        void InitializeIfNeeded(IConfiguration userDefinedConfig = null)
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

                userDefinedConfig.HttpClient = httpClient;

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
