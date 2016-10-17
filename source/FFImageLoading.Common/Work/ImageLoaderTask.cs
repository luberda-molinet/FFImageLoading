using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Config;
using System.Linq;

namespace FFImageLoading.Work
{
    public interface IImageLoaderTask : IScheduledWork, IDisposable
    {
        TaskParameter Parameters { get; }

        bool CanUseMemoryCache { get; }

        string Key { get; }

        string KeyRaw { get; }

        void CancelIfNeeded();

        Task<bool> TryLoadFromMemoryCacheAsync();

        Task RunAsync();

        bool UsesSameNativeControl(IImageLoaderTask anotherTask);
    }

    public abstract class ImageLoaderTask<TImageContainer> : IImageLoaderTask
    {
        static int _streamIndex;
        static int GetNextStreamIndex()
        {
            return Interlocked.Increment(ref _streamIndex);
        }

        readonly bool _clearCacheOnOutOfMemory;

        public ImageLoaderTask(ITarget<TImageContainer> target, IDataResolverFactory dataResolverFactory, TaskParameter parameters, IImageService imageService, Configuration configuration, IMainThreadDispatcher mainThreadDispatcher, bool clearCacheOnOutOfMemory)
        {
            _clearCacheOnOutOfMemory = clearCacheOnOutOfMemory;
            Target = target;
            DataResolverFactory = dataResolverFactory;
            ImageService = imageService;
            Configuration = configuration;
            MainThreadDispatcher = mainThreadDispatcher;
            Parameters = parameters;
            CancellationTokenSource = new CancellationTokenSource();
            ImageInformation = new ImageInformation();
            CanUseMemoryCache = true;

            KeyRaw = Parameters.Path;
            if (Parameters.Source == ImageSource.Stream)
            {
                CanUseMemoryCache = false;
                KeyRaw = string.Concat("Stream_", GetNextStreamIndex());
            }

            if (!string.IsNullOrWhiteSpace(Parameters.CustomCacheKey))
            {
                CanUseMemoryCache = true;
                KeyRaw = Parameters.CustomCacheKey;
            }

            KeyDownsamplingOnly = string.Empty;
            if (Parameters.DownSampleSize != null)
            {
                KeyDownsamplingOnly = string.Concat(";", Parameters.DownSampleSize.Item1, "x", Parameters.DownSampleSize.Item2);
            }

            KeyTransformationsOnly = string.Empty;
            if (Parameters.Transformations != null && Parameters.Transformations.Count > 0)
            {
                KeyTransformationsOnly = string.Concat(string.Join(";", Parameters.Transformations.Select(t => t.Key)));
            }

            Key = string.Concat(KeyRaw, KeyDownsamplingOnly, KeyTransformationsOnly);
            KeyWithoutTransformations = string.Concat(KeyRaw, KeyDownsamplingOnly);

            if (!string.IsNullOrWhiteSpace(Parameters.LoadingPlaceholderPath))
            {
                if (TransformPlaceholders)
                    KeyForLoadingPlaceholder = string.Concat(Parameters.LoadingPlaceholderPath, KeyDownsamplingOnly, KeyTransformationsOnly);
                else
                    KeyForLoadingPlaceholder = string.Concat(Parameters.LoadingPlaceholderPath, KeyDownsamplingOnly);
            }

            if (!string.IsNullOrWhiteSpace(Parameters.ErrorPlaceholderPath))
            {
                if (TransformPlaceholders)
                    KeyForErrorPlaceholder = string.Concat(Parameters.ErrorPlaceholderPath, KeyDownsamplingOnly, KeyTransformationsOnly);
                else
                    KeyForErrorPlaceholder = string.Concat(Parameters.ErrorPlaceholderPath, KeyDownsamplingOnly);
            }
        }

        protected ITarget<TImageContainer> Target { get; private set; }

        protected IDataResolverFactory DataResolverFactory { get; private set; }

        protected IImageService ImageService { get; private set; }

        protected Configuration Configuration { get; private set; }

        protected IMemoryCache<TImageContainer> MemoryCache { get; private set; }

        protected IDiskCache DiskCache { get { return Configuration.DiskCache; } }

        protected IDownloadCache DownloadCache { get { return Configuration.DownloadCache; } }

        protected IMiniLogger Logger { get { return Configuration.Logger; } }

        protected ImageInformation ImageInformation { get; private set; }

        protected CancellationTokenSource CancellationTokenSource { get; private set; }

        protected CancellationToken CancellationToken { get { return CancellationTokenSource.Token; } }

        protected IMainThreadDispatcher MainThreadDispatcher { get; private set; }

        public bool IsCancelled
        {
            get
            {
                try
                {
                    return _isDisposed || (CancellationTokenSource != null && CancellationTokenSource.IsCancellationRequested);
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
            }
        }

        public bool CanUseMemoryCache { get; private set; }

        public bool Completed { get; private set; }

        public string Key { get; private set; }

        public string KeyRaw { get; private set; }

        public string KeyWithoutTransformations { get; private set; }

        protected string KeyTransformationsOnly { get; private set; }

        protected string KeyDownsamplingOnly { get; private set; }

        protected string KeyForLoadingPlaceholder { get; private set; }

        protected string KeyForErrorPlaceholder { get; private set; }

        protected bool TransformPlaceholders
        {
            get
            {
                return (Parameters.TransformPlaceholdersEnabled.HasValue && Parameters.TransformPlaceholdersEnabled.Value)
                    || (!Parameters.TransformPlaceholdersEnabled.HasValue && Configuration.TransformPlaceholders);
            }
        }

        public TaskParameter Parameters { get; private set; }

        public virtual bool UsesSameNativeControl(IImageLoaderTask anotherTask)
        {
            return Target.UsesSameNativeControl(anotherTask);
        }

        public void Cancel()
        {
            ImageService.RemovePendingTask(this);

            if (!_isDisposed)
            {
                try
                {
                    CancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            if (Configuration.VerboseLoadingCancelledLogging)
                Logger.Debug(string.Format("Canceled image generation for {0}", Key));
        }

        public void CancelIfNeeded()
        {
            if (!IsCancelled && !Completed)
                Cancel();
        }

        protected abstract Task<Tuple<TImageContainer, ImageInformation>> GenerateImageAsync(Stream imageData, bool enableTransformations);

        protected abstract Task SetTargetAsync(TImageContainer image, bool animated);

        public async Task<bool> TryLoadFromMemoryCacheAsync()
        {
            var result = await TryLoadFromMemoryCacheAsync(Key, true);

            if (result)
            {
                Logger.Debug(string.Format("Image loaded from cache: {0}", Key));
                Parameters?.OnSuccess?.Invoke(ImageInformation, LoadingResult.MemoryCache);
                Parameters?.OnFinish?.Invoke(this);
            }

            return result;
        }

        async Task<bool> TryLoadFromMemoryCacheAsync(string key, bool updateImageInformation)
        {
            var found = MemoryCache.Get(key);

            if (found != null)
            {
                await SetTargetAsync(found.Item1, false).ConfigureAwait(false);

                if (updateImageInformation)
                    ImageInformation = found.Item2;

                return true;
            }

            return false;
        }

        async Task ShowPlaceholder(string path, string key, ImageSource source)
        {
            if (!await TryLoadFromMemoryCacheAsync(key, false).ConfigureAwait(false))
            {
                var loadResolver = DataResolverFactory.GetResolver(path, source, Parameters);
                var loadImageData = await loadResolver.Resolve(path, Parameters, Configuration, 
                                                               CancellationTokenSource.Token).ConfigureAwait(false);

                CancellationToken.ThrowIfCancellationRequested();

                var loadImage = await GenerateImageAsync(loadImageData.Item1, TransformPlaceholders).ConfigureAwait(false);

                if (loadImage != null)
                    MemoryCache.Add(key, loadImage.Item2, loadImage.Item1);

                CancellationToken.ThrowIfCancellationRequested();

                await SetTargetAsync(loadImage.Item1, false).ConfigureAwait(false);
            }
        }

        public async Task RunAsync()
        {
            try
            {
                if (Completed || IsCancelled || ImageService.ExitTasksEarly)
                    throw new OperationCanceledException();

                CancellationToken.ThrowIfCancellationRequested();

                LoadingResult loadingResult = LoadingResult.Failed;

                // Loading placeholder if enabled
                if (!string.IsNullOrWhiteSpace(Parameters.LoadingPlaceholderPath))
                {
                    await ShowPlaceholder(Parameters.LoadingPlaceholderPath, KeyForLoadingPlaceholder, 
                                          Parameters.LoadingPlaceholderSource).ConfigureAwait(false);
                }

                CancellationToken.ThrowIfCancellationRequested();

                if (!(await TryLoadFromMemoryCacheAsync()))
                {
                    Logger.Debug(string.Format("Generating/retrieving image: {0}", Key));
                    var resolver = DataResolverFactory.GetResolver(Parameters.Path, Parameters.Source, Parameters);
                    var imageData = await resolver.Resolve(Parameters.Path, Parameters,
                                                           Configuration, CancellationTokenSource.Token).ConfigureAwait(false);

                    CancellationToken.ThrowIfCancellationRequested();

                    var image = await GenerateImageAsync(imageData.Item1, TransformPlaceholders).ConfigureAwait(false);
                    ImageInformation = image.Item2;
                }

                Parameters?.OnSuccess?.Invoke(ImageInformation, loadingResult);
            }
            catch (Exception ex)
            {
                if (_clearCacheOnOutOfMemory && ex is OutOfMemoryException)
                {
                    MemoryCache.Clear();
                }

                if (!(ex is OperationCanceledException))
                {
                    Logger.Error(string.Format("Image loading failed: ", Key), ex);

                    // Error placeholder if enabled
                    if (!string.IsNullOrWhiteSpace(Parameters.ErrorPlaceholderPath))
                    {
                        await ShowPlaceholder(Parameters.ErrorPlaceholderPath, KeyForErrorPlaceholder, 
                                              Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
                    }

                    Parameters?.OnError?.Invoke(ex);
                }
            }
            finally
            {
                using (Parameters)
                {
                    Parameters?.OnFinish?.Invoke(this);
                    ImageService.RemovePendingTask(this);
                }
            }
        }

        bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                try
                {
                    Parameters?.Dispose();
                    CancellationTokenSource?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }

                _isDisposed = true;
            }
        }
    }
}
