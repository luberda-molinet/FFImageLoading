using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Config;

namespace FFImageLoading.Work
{
	public class TaskParameter : IDisposable
	{
		private bool _disposed;

        private TaskParameter()
        {
            Transformations = new List<ITransformation>();
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a file.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="filepath">Path to the file.</param>
        public static TaskParameter FromFile(string filepath)
        {
            return new TaskParameter() { Source = ImageSource.Filepath, Path = filepath };
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a file.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="resourceUri">Uri to the resource.</param>
        public static TaskParameter FromEmbeddedResource(string resourceUri)
		{
            if (!resourceUri.StartsWith("resource://", StringComparison.OrdinalIgnoreCase))
                resourceUri = $"resource://{resourceUri}";

            return new TaskParameter() { Source = ImageSource.EmbeddedResource, Path = resourceUri }; 
		}

		/// <summary>
		/// Constructs a new TaskParameter to load an image from a URL.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="url">URL to the file</param>
		/// <param name="cacheDuration">How long the file will be cached on disk</param>
		public static TaskParameter FromUrl(string url, TimeSpan? cacheDuration = null)
		{
			return new TaskParameter() { Source = ImageSource.Url, Path = url, CacheDuration = cacheDuration };
		}

		/// <summary>
		/// Constructsa new TaskParameter to load an image from a file from application bundle.
		/// </summary>
		/// <param name="filepath">Path to the file.</param>
		/// <returns>The new TaskParameter.</returns>
		public static TaskParameter FromApplicationBundle(string filepath)
		{
			var taskParameter = new TaskParameter() { Source = ImageSource.ApplicationBundle, Path = filepath };

            if (!taskParameter.Priority.HasValue)
                taskParameter.Priority = (int)LoadingPriority.Normal + 1;

            return taskParameter;
		}

		/// <summary>
		/// Constructs a new TaskParameter to load an image from a compiled drawable resource.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="resourceName">Name of the resource in drawable folder without extension</param>
		public static TaskParameter FromCompiledResource(string resourceName)
		{
			var taskParameter = new TaskParameter() { Source = ImageSource.CompiledResource, Path = resourceName };

            if (!taskParameter.Priority.HasValue)
                taskParameter.Priority = (int)LoadingPriority.Normal + 1;

            return taskParameter;
		}

		/// <summary>
		/// Constructs a new TaskParameter to load an image from a stream
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="stream">Stream.</param>
		public static TaskParameter FromStream(Func<CancellationToken, Task<Stream>> stream)
		{
			return new TaskParameter() { Source = ImageSource.Stream, Stream = stream };
		}

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a string.
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="data">Data.</param>
        /// <param name="dataEncoding">Data encoding.</param>
        public static TaskParameter FromString(string data, DataEncodingType encoding)
        {
            return new TaskParameter() { Source = ImageSource.Url, Path = data, DataEncoding = encoding };
        }

		public ImageSource Source { get; private set; }

		public string Path { get; private set; }

		public Func<CancellationToken, Task<Stream>> Stream { get; private set; }

        internal Stream StreamRead { get; set; }

        internal string StreamChecksum { get; set; }

		public TimeSpan? CacheDuration { get; private set; }

		public Tuple<int, int> DownSampleSize { get; private set; }

		public bool DownSampleUseDipUnits { get; private set; }

		public bool? AllowUpscale { get; private set; }

        public InterpolationMode DownSampleInterpolationMode { get; private set; }

        public ImageSource LoadingPlaceholderSource { get; private set; }

		public string LoadingPlaceholderPath { get; private set; }

		public ImageSource ErrorPlaceholderSource { get; private set; }

		public string ErrorPlaceholderPath { get; private set; }

		public int RetryCount { get; private set; }

		public int RetryDelayInMs { get; private set; }

        public Action<ImageInformation, LoadingResult> OnSuccess { get; private set; }

		public Action<Exception> OnError { get; private set; }

		public Action<IScheduledWork> OnFinish { get; private set; }

        public Action<DownloadInformation> OnDownloadStarted { get; private set; }

        public Action<FileWriteInfo> OnFileWriteFinished { get; private set; }

        public Action<DownloadProgress> OnDownloadProgress { get; private set; }

		public List<ITransformation> Transformations { get; private set; }

        [Obsolete("Use BitmapOptimizations")]
		public bool? LoadTransparencyChannel { get; private set; }

        public bool? BitmapOptimizationsEnabled { get; private set; }

		public bool? FadeAnimationEnabled { get; private set; }

        public IDataResolver CustomDataResolver { get; private set; }

        public IDataResolver CustomErrorPlaceholderDataResolver { get; private set; }

        public IDataResolver CustomLoadingPlaceholderDataResolver { get; private set; }

		public bool? FadeAnimationForCachedImagesEnabled { get; private set; }

		public int? FadeAnimationDuration { get; private set; }

        public bool? TransformPlaceholdersEnabled { get; private set; }

		public string CustomCacheKey { get; private set; }

		public int? Priority { get; private set; }

        public CacheType? CacheType { get; private set; }

        public DataEncodingType DataEncoding { get; private set; } = DataEncodingType.RAW;

		/// <summary>
		/// Gets the delay in milliseconds.
		/// </summary>
		/// <value>The delay in milliseconds.</value>
		public int? DelayInMs { get; private set; }

        public bool Preload { get; internal set; }

        public TaskParameter Transform(ITransformation transformation)
		{
			if (transformation == null)
				throw new NullReferenceException("The transformation argument was null.");

			Transformations.Add(transformation);
			return this;
		}

		public TaskParameter Transform(IEnumerable<ITransformation> transformations)
		{
			if (transformations == null)
				throw new ArgumentNullException("The transformations argument was null");

			Transformations.AddRange(transformations);
			return this;
		}

		/// <summary>
		/// Defines the placeholder used while loading.
		/// </summary>
		/// <param name="path">Path to the file.</param>
		/// <param name="source">Source for the path: local, web, assets</param>
		public TaskParameter LoadingPlaceholder(string path, ImageSource source = ImageSource.Filepath)
		{
			LoadingPlaceholderPath = path;
			LoadingPlaceholderSource = source;
			return this;
		}

		/// <summary>
		/// Defines the placeholder used when an error occurs.
		/// </summary>
		/// <param name="filepath">Path to the file.</param>
		/// <param name="source">Source for the path: local, web, assets</param>
		public TaskParameter ErrorPlaceholder(string filepath, ImageSource source = ImageSource.Filepath)
		{
			ErrorPlaceholderPath = filepath;
			ErrorPlaceholderSource = source;
			return this;
		}

		/// <summary>
		/// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect.
		/// Uses pixels units for width/height
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="width">Optional width parameter, if value is higher than zero it will try to downsample to this width while keeping aspect ratio.</param>
		/// <param name="height">Optional height parameter, if value is higher than zero it will try to downsample to this height while keeping aspect ratio.</param>
        /// <param name="allowUpscale">Whether to upscale the image if it is smaller than passed dimensions or not; if <c>null</c> the value is taken from Configuration (<c>false</c> by default)</param>
        public TaskParameter DownSample(int width = 0, int height = 0, bool? allowUpscale = null)
		{
			DownSampleUseDipUnits = false;
			DownSampleSize = Tuple.Create(width, height);
		    AllowUpscale = allowUpscale;

			return this;
		}

		/// <summary>
		/// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect.
		/// Uses device independent points units for width/height
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="width">Optional width parameter, if value is higher than zero it will try to downsample to this width while keeping aspect ratio.</param>
		/// <param name="height">Optional height parameter, if value is higher than zero it will try to downsample to this height while keeping aspect ratio.</param>
        /// <param name="allowUpscale">Whether to upscale the image if it is smaller than passed dimensions or not; if <c>null</c> the value is taken from Configuration (<c>false</c> by default)</param>
        public TaskParameter DownSampleInDip(int width = 0, int height = 0, bool? allowUpscale = null)
		{
			DownSampleUseDipUnits = true;
			DownSampleSize = Tuple.Create(width, height);
		    AllowUpscale = allowUpscale;

			return this;
		}

        /// <summary>
        /// Set mode for downsampling. Speed-wise: nearest neighbour > linear > cubic.\
		/// Default: bilinear
		/// On Android it's always ignored as Android uses bitmap insamplesize downsampling (bilinear)
        /// </summary>
        /// <returns>The TaskParameter instance for chaining the call.</returns>
        /// <param name="mode">Optional mode parameter, if not set, defaults to linear.</param>
        public TaskParameter DownSampleMode(InterpolationMode mode)
        {
            DownSampleInterpolationMode = mode;
            return this;
        }

		/// <summary>
		/// Defines the loading priority, the default is LoadingPriority.Normal
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="priority">Priority.</param>
		public TaskParameter WithPriority(LoadingPriority priority)
		{
			Priority = (int)priority;
			return this;
		}

        /// <summary>
        /// Forces task to use custom resolver.
        /// </summary>
        /// <returns>The custom resolver.</returns>
        /// <param name="resolver">Resolver.</param>
        public TaskParameter WithCustomDataResolver(IDataResolver resolver = null)
        {
            CustomDataResolver = resolver;
            return this;
        }

        public TaskParameter WithCustomLoadingPlaceholderDataResolver(IDataResolver resolver = null)
        {
            CustomLoadingPlaceholderDataResolver = resolver;
            return this;
        }

        public TaskParameter WithCustomErrorPlaceholderDataResolver(IDataResolver resolver = null)
        {
            CustomErrorPlaceholderDataResolver = resolver;
            return this;
        }

		/// <summary>
		/// Defines the loading priority, the default is 0 (LoadingPriority.Normal)
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="priority">Priority.</param>
		public TaskParameter WithPriority(int priority)
		{
			Priority = priority;
			return this;
		}

        public TaskParameter WithCache(CacheType cacheType)
        {
            CacheType = cacheType;
            return this;
        }

		/// <summary>
		/// Indicates if the transparency channel should be loaded. By default this value comes from ImageService.Instance.Config.LoadWithTransparencyChannel.
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="loadTransparencyChannel">If set to <c>true</c> force loading alpha channel otherwise force not loading it.</param>
        [Obsolete]
        public TaskParameter TransparencyChannel(bool loadTransparencyChannel)
		{
			LoadTransparencyChannel = loadTransparencyChannel;
			return this;
		}

        public TaskParameter BitmapOptimizations(bool enabled)
        {
            BitmapOptimizationsEnabled = enabled;
            return this;
        }

		/// <summary>
		/// Indicates if the fade animation should be enabled.
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="enabled">If set to <c>true</c> enabled.</param>
		/// <param name = "enabledForCachedImages">Enables animation for local or cached images</param>
		/// <param name = "duration">Fade animation duration in ms</param>
		public TaskParameter FadeAnimation(bool enabled, bool? enabledForCachedImages = null, int? duration = null)
		{
			FadeAnimationEnabled = enabled;
			FadeAnimationForCachedImagesEnabled = enabledForCachedImages;
			FadeAnimationDuration = duration;

			return this;
		}

        /// <summary>
        /// Indicates if transforms should be applied to placeholders.
        /// </summary>
        /// <returns>The TaskParameter instance for chaining the call.</returns>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        public TaskParameter TransformPlaceholders(bool enabled)
        {
            TransformPlaceholdersEnabled = enabled;
            return this;
        }

        /// <summary>
        /// If image loading fails automatically retry it a number of times, with a specific delay.
        /// </summary>
        /// <returns>The TaskParameter instance for chaining the call.</returns>
        /// <param name="retryCount">Number of retries</param>
        /// <param name="millisecondDelay">Delay in milliseconds between each trial</param>
        public TaskParameter Retry(int retryCount = 0, int millisecondDelay = 0)
		{
			RetryCount = retryCount;
			RetryDelayInMs = millisecondDelay;
			return this;
		}

		/// <summary>
		/// Uses this cache key, in addition with the real key, to cache into memory/disk
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="customCacheKey">Custom cache key.</param>
		public TaskParameter CacheKey(string customCacheKey)
		{
			CustomCacheKey = customCacheKey;
			return this;
		}

		/// <summary>
		/// Delay the task by the specified milliseconds.
		/// </summary>
		/// <param name="milliseconds">Milliseconds to wait prior to start the task.</param>
		public TaskParameter Delay(int milliseconds)
		{
			DelayInMs = milliseconds;
			return this;
		}

		/// <summary>
		/// If image loading succeded this callback is called
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="action">Action to invoke when loading succeded.</param>
		public TaskParameter Success(Action action)
		{
			if (action == null)
				throw new Exception("Given lambda should not be null.");

			OnSuccess = (s, r) => action();
			return this;
		}

		/// <summary>
		/// If image loading succeded this callback is called
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="action">Action to invoke when loading succeded. Argument is the size of the image loaded.</param>
		public TaskParameter Success(Action<ImageInformation, LoadingResult> action)
		{
			if (action == null)
				throw new Exception("Given lambda should not be null.");

			OnSuccess = action;
			return this;
		}

		/// <summary>
		/// If image loading failed this callback is called
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="action">Action to invoke when loading failed
		public TaskParameter Error(Action<Exception> action)
		{
			if (action == null)
				throw new Exception("Given lambda should not be null.");

			OnError = action;
			return this;
		}

		/// <summary>
		/// If image loading process finished, whatever the result, this callback is called
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="action">Action to invoke when process is done
		public TaskParameter Finish(Action<IScheduledWork> action)
		{
			if (action == null)
				throw new Exception("Given lambda should not be null.");

			OnFinish = action;
			return this;
		}

        /// <summary>
        /// If image starts downloading from Internet this callback is called
        /// </summary>
        /// <returns>The TaskParameter instance for chaining the call.</returns>
        /// <param name="action">Action.</param>
        public TaskParameter DownloadStarted(Action<DownloadInformation> action)
        {
            if (action == null)
                throw new Exception("Given lambda should not be null.");

            OnDownloadStarted = action;
            return this;
        }

        /// <summary>
        /// This callback can be used for reading download progress
        /// </summary>
        /// <returns>The progress.</returns>
        /// <param name="action">Action.</param>
        public TaskParameter DownloadProgress(Action<DownloadProgress> action)
        {
            if (action == null)
                throw new Exception("Given lambda should not be null.");

            OnDownloadProgress = action;
            return this;
        }

        /// <summary>
        /// Called after file is succesfully written to the disk
        /// </summary>
        /// <returns>The write ended.</returns>
        /// <param name="action">Action.</param>
        public TaskParameter FileWriteFinished(Action<FileWriteInfo> action)
        {
            if (action == null)
                throw new Exception("Given lambda should not be null.");

            OnFileWriteFinished = action;
            return this;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                OnSuccess = null;
                OnError = null;
                OnFinish = null;
                OnDownloadStarted = null;
                OnDownloadProgress = null;
                OnFileWriteFinished = null;
                Transformations = null;
                Stream = null;

                _disposed = true;
            }
        }
    }
}

