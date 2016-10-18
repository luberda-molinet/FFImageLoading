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
    public abstract class ImageLoaderTask<TImageContainer, TImageView> : IImageLoaderTask where TImageContainer: class where TImageView: class
    {
        static int _streamIndex;
        static int GetNextStreamIndex()
        {
            return Interlocked.Increment(ref _streamIndex);
        }

        readonly bool _clearCacheOnOutOfMemory;

        public ImageLoaderTask(IMemoryCache<TImageContainer> memoryCache, IDataResolverFactory dataResolverFactory, ITarget<TImageContainer, TImageView> target, TaskParameter parameters, IImageService imageService, Configuration configuration, IMainThreadDispatcher mainThreadDispatcher, bool clearCacheOnOutOfMemory)
        {
            _clearCacheOnOutOfMemory = clearCacheOnOutOfMemory;
            MemoryCache = memoryCache;
            DataResolverFactory = dataResolverFactory;
            TargetNative = target;
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

            ImageInformation.SetKey(Key, Parameters.CustomCacheKey);
            ImageInformation.SetPath(Parameters.Path);
        }

        public Configuration Configuration { get; private set; }

        public ImageInformation ImageInformation { get; private set; }

        public DownloadInformation DownloadInformation { get; private set; }

        public CancellationToken CancellationToken { get { return CancellationTokenSource.Token; } }

        public ITarget<TImageContainer, TImageView> TargetNative { get; private set; }

        public ITarget Target { get { return TargetNative as ITarget; } }

        protected IImageService ImageService { get; private set; }

        protected IMemoryCache<TImageContainer> MemoryCache { get; private set; }

        protected IDataResolverFactory DataResolverFactory { get; private set; }

        protected IDiskCache DiskCache { get { return Configuration.DiskCache; } }

        protected IDownloadCache DownloadCache { get { return Configuration.DownloadCache; } }

        protected IMiniLogger Logger { get { return Configuration.Logger; } }

        protected CancellationTokenSource CancellationTokenSource { get; private set; }

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
                Logger.Debug(string.Format("Image loading cancelled: {0}", Key));
        }

        public void CancelIfNeeded()
        {
            if (!IsCancelled && !Completed)
                Cancel();
        }

        protected abstract Task<TImageContainer> GenerateImageAsync(string path, Stream imageData, ImageInformation imageInformation, bool enableTransformations);

        protected abstract Task SetTargetAsync(TImageContainer image, bool animated);

        public async Task<bool> TryLoadFromMemoryCacheAsync()
        {
            try
            {
                if (Parameters.Preload && Parameters.CacheType.HasValue && Parameters.CacheType.Value == CacheType.Disk)
                    return false;

                bool isFadeAnimationEnabledForCached = Parameters.FadeAnimationForCachedImages ?? Configuration.FadeAnimationForCachedImages;
                var result = await TryLoadFromMemoryCacheAsync(Key, true, isFadeAnimationEnabledForCached).ConfigureAwait(false);

                if (result)
                {
                    Logger.Debug(string.Format("Image loaded from cache: {0}", Key));
                    Parameters?.OnSuccess?.Invoke(ImageInformation, LoadingResult.MemoryCache);
                    Parameters?.OnFinish?.Invoke(this);
                    Completed = true;
                }
                else
                {
                    // Loading placeholder if enabled
                    if (!string.IsNullOrWhiteSpace(Parameters.LoadingPlaceholderPath))
                    {
                        await ShowPlaceholder(Parameters.LoadingPlaceholderPath, KeyForLoadingPlaceholder,
                                              Parameters.LoadingPlaceholderSource).ConfigureAwait(false);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                if (_clearCacheOnOutOfMemory && ex is OutOfMemoryException)
                {
                    MemoryCache.Clear();
                }

                if (ex is OperationCanceledException)
                {
                    if (Configuration.VerboseLoadingCancelledLogging)
                    {
                        Logger.Debug(string.Format("Image loading cancelled: {0}", Key));
                    }
                }
                else
                {
                    Logger.Error(string.Format("Image loading failed: {0}", Key), ex);
                    Parameters?.OnError?.Invoke(ex);
                }
            }

            return false;
        }

        async Task<bool> TryLoadFromMemoryCacheAsync(string key, bool updateImageInformation, bool animated)
        {
            var found = MemoryCache.Get(key);

            if (found?.Item1 != null)
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
            if (!await TryLoadFromMemoryCacheAsync(key, false, false).ConfigureAwait(false))
            {
                var loadResolver = DataResolverFactory.GetResolver(path, source, Parameters, Configuration);
                var loadImageData = await loadResolver.Resolve(path, Parameters, CancellationToken).ConfigureAwait(false);

                using (loadImageData.Item1)
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    var loadImage = await GenerateImageAsync(path, loadImageData.Item1, loadImageData.Item3, TransformPlaceholders).ConfigureAwait(false);

                    if (loadImage != default(TImageContainer))
                        MemoryCache.Add(key, loadImageData.Item3, loadImage);

                    CancellationToken.ThrowIfCancellationRequested();

                    await SetTargetAsync(loadImage, false).ConfigureAwait(false);
                }
            }
        }

        //async Task<Tuple<bool, LoadingResult>> TryDownloadOnlyAsync()
        //{
        //    try
        //    {
        //        if (Parameters.Source != ImageSource.Url)
        //            throw new InvalidOperationException("DownloadOnly: Only Url ImageSource is supported.");

        //        var data = await DownloadCache.DownloadAndCacheIfNeededAsync(Parameters.Path, Parameters, Configuration, CancellationToken).ConfigureAwait(false);
        //        using (var imageStream = data.ImageStream)
        //        {
        //            if (!data.RetrievedFromDiskCache)
        //                Logger?.Debug(string.Format("DownloadOnly success: {0}", Key));
        //        }

        //        return new Tuple<bool, LoadingResult>(true, data.RetrievedFromDiskCache ? LoadingResult.DiskCache : LoadingResult.Internet);
        //    }
        //    catch (Exception ex)
        //    {
        //        if (!(ex is OperationCanceledException))
        //        {
        //            Logger?.Error(string.Format("DownloadOnly failed: {0}", Key), ex);
        //        }
        //    }

        //    return new Tuple<bool, LoadingResult>(false, LoadingResult.Failed);
        //}

        public async Task RunAsync()
        {
            try
            {
                if (Completed || IsCancelled || ImageService.ExitTasksEarly)
                    throw new OperationCanceledException();

                CancellationToken.ThrowIfCancellationRequested();
                LoadingResult loadingResult = LoadingResult.Failed;

                // LOAD IMAGE
                if (!(await TryLoadFromMemoryCacheAsync().ConfigureAwait(false)))
                {
                    Logger.Debug(string.Format("Generating/retrieving image: {0}", Key));
                    var resolver = DataResolverFactory.GetResolver(Parameters.Path, Parameters.Source, Parameters, Configuration);
                    var imageData = await resolver.Resolve(Parameters.Path, Parameters, CancellationToken).ConfigureAwait(false);
                    loadingResult = imageData.Item2;

                    using (imageData.Item1)
                    {
                        ImageInformation = imageData.Item3;
                        CancellationToken.ThrowIfCancellationRequested();

                        // Preload
                        if (Parameters.Preload && Parameters.CacheType.HasValue && Parameters.CacheType.Value == CacheType.Disk)
                        {
                            if (Parameters.Source != ImageSource.Url)
                                throw new InvalidOperationException("DownloadOnly: Only Url ImageSource is supported.");

                            if (loadingResult == LoadingResult.Internet)
                                Logger?.Debug(string.Format("DownloadOnly success: {0}", Key));
                            
                            Parameters?.OnSuccess?.Invoke(ImageInformation, loadingResult);
                            return;
                        }

                        CancellationToken.ThrowIfCancellationRequested();

                        var image = await GenerateImageAsync(Parameters.Path, imageData.Item1, imageData.Item3, TransformPlaceholders).ConfigureAwait(false);

                        if (image != default(TImageContainer) && CanUseMemoryCache)
                            MemoryCache.Add(Key, imageData.Item3, image);

                        CancellationToken.ThrowIfCancellationRequested();

                        bool isFadeAnimationEnabled = Parameters.FadeAnimationEnabled ?? Configuration.FadeAnimationEnabled;
                        await SetTargetAsync(image, isFadeAnimationEnabled).ConfigureAwait(false);
                    }
                }

                Parameters?.OnSuccess?.Invoke(ImageInformation, loadingResult);
            }
            catch (Exception ex)
            {
                if (_clearCacheOnOutOfMemory && ex is OutOfMemoryException)
                {
                    MemoryCache.Clear();
                }

                if (ex is OperationCanceledException)
                {
                    if (Configuration.VerboseLoadingCancelledLogging)
                    {
                        Logger.Debug(string.Format("Image loading cancelled: {0}", Key));
                    }
                }
                else
                {
                    Logger.Error(string.Format("Image loading failed: {0}", Key), ex);
                    Parameters?.OnError?.Invoke(ex);

                    try
                    {
                        // Error placeholder if enabled
                        if (!Parameters.Preload && !string.IsNullOrWhiteSpace(Parameters.ErrorPlaceholderPath))
                        {
                            await ShowPlaceholder(Parameters.ErrorPlaceholderPath, KeyForErrorPlaceholder,
                                                  Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex2)
                    {
                        if (!(ex2 is OperationCanceledException))
                        {
                            Logger.Error(string.Format("Image loading failed: {0}", Key), ex);
                        }
                    }
                }
            }
            finally
            {
                using (Parameters)
                {
                    Parameters?.OnFinish?.Invoke(this);
                    ImageService.RemovePendingTask(this);
                }

                Completed = true;
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
