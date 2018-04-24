using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Config;
using System.Linq;
using FFImageLoading.DataResolvers;
using System.Collections.Generic;
using FFImageLoading.Decoders;

namespace FFImageLoading.Work
{
    public abstract class ImageLoaderTask<TDecoderContainer, TImageContainer, TImageView> : IImageLoaderTask where TDecoderContainer : class where TImageContainer : class where TImageView : class
    {
        bool _isLoadingPlaceholderLoaded;
        static readonly SemaphoreSlim _placeholdersResolveLock = new SemaphoreSlim(1, 1);

        public ImageLoaderTask(IMemoryCache<TImageContainer> memoryCache, ITarget<TImageContainer, TImageView> target, TaskParameter parameters, IImageService imageService)
        {
            MemoryCache = memoryCache;
            DataResolverFactory = imageService.Config.DataResolverFactory;
            PlatformTarget = target;
            ImageService = imageService;
            Configuration = imageService.Config;
            Parameters = parameters;
            CancellationTokenSource = new CancellationTokenSource();
            ImageInformation = new ImageInformation();
            CanUseMemoryCache = true;
            SetKeys();
        }

        public virtual async Task Init()
        {
            if (Parameters.Source == ImageSource.Stream && Configuration.StreamChecksumsAsKeys && string.IsNullOrWhiteSpace(Parameters.CustomCacheKey))
            {
                // Loading placeholder if enabled
                if (!_isLoadingPlaceholderLoaded && !string.IsNullOrWhiteSpace(Parameters.LoadingPlaceholderPath))
                {
                    await ShowPlaceholder(Parameters.LoadingPlaceholderPath, KeyForLoadingPlaceholder,
                                          Parameters.LoadingPlaceholderSource, true).ConfigureAwait(false);
                }

                try
                {
                    Parameters.StreamRead = await (Parameters.Stream?.Invoke(CancellationTokenSource.Token)).ConfigureAwait(false);
                }
                catch (TaskCanceledException ex)
                {
                    Parameters.StreamRead = null;
                    Logger.Error(ex.Message, ex);
                }

                if (Parameters.StreamRead != null && Parameters.StreamRead.CanSeek)
                {
                    Parameters.StreamChecksum = Configuration.MD5Helper.MD5(Parameters.StreamRead);
                    Parameters.StreamRead.Position = 0;

                    SetKeys();
                }
            }
        }

        void SetKeys()
        {
            KeyRaw = Parameters.Path;
            if (Parameters.Source == ImageSource.Stream)
            {
                if (!string.IsNullOrWhiteSpace(Parameters.StreamChecksum))
                {
                    CanUseMemoryCache = true;
                    KeyRaw = Parameters.StreamChecksum;
                }
                else
                {
                    CanUseMemoryCache = false;
                    KeyRaw = string.Concat("Stream_", Guid.NewGuid().ToString("N"));
                }
            }

            if (!string.IsNullOrWhiteSpace(Parameters.CustomCacheKey))
            {
                CanUseMemoryCache = true;
                KeyRaw = Parameters.CustomCacheKey;
            }

            if (Parameters.CacheType == CacheType.Disk || Parameters.CacheType == CacheType.None)
            {
                CanUseMemoryCache = false;
            }

            if (string.IsNullOrWhiteSpace(KeyRaw))
                throw new Exception("Key cannot be null");

            var vect = Parameters.CustomDataResolver as IVectorDataResolver;
            if (vect != null)
            {
                if (vect.ReplaceStringMap == null || vect.ReplaceStringMap.Count == 0)
                    KeyRaw = string.Format("{0};(size={1}x{2},dip={3},type={4})", KeyRaw, vect.VectorWidth, vect.VectorHeight, vect.UseDipUnits, vect.GetType().Name);
                else
                    KeyRaw = string.Format("{0};(size={1}x{2},dip={3},replace=({4}),type={5})", KeyRaw, vect.VectorWidth, vect.VectorHeight, vect.UseDipUnits,
                                           string.Join(",", vect.ReplaceStringMap.Select(x => string.Format("{0}/{1}", x.Key, x.Value)).OrderBy(v => v)), vect.GetType().Name);
            }

            KeyDownsamplingOnly = string.Empty;
            if (Parameters.DownSampleSize != null && (Parameters.DownSampleSize.Item1 > 0 || Parameters.DownSampleSize.Item2 > 0))
            {
                if (Parameters.DownSampleUseDipUnits)
                    KeyDownsamplingOnly = string.Concat(";", DpiToPixels(Parameters.DownSampleSize.Item1), "x", DpiToPixels(Parameters.DownSampleSize.Item2));
                else
                    KeyDownsamplingOnly = string.Concat(";", Parameters.DownSampleSize.Item1, "x", Parameters.DownSampleSize.Item2);
            }

            KeyTransformationsOnly = string.Empty;
            if (Parameters.Transformations != null && Parameters.Transformations.Count > 0)
            {
                KeyTransformationsOnly = string.Concat(";", string.Join(";", Parameters.Transformations.Select(t => t.Key)));
            }

            Key = string.Concat(KeyRaw, KeyDownsamplingOnly, KeyTransformationsOnly);
            KeyWithoutTransformations = string.Concat(KeyRaw, KeyDownsamplingOnly);

            if (!string.IsNullOrWhiteSpace(Parameters.LoadingPlaceholderPath))
            {
                if (TransformPlaceholders)
                    KeyForLoadingPlaceholder = string.Concat(Parameters.LoadingPlaceholderPath, KeyDownsamplingOnly, KeyTransformationsOnly);
                else
                    KeyForLoadingPlaceholder = string.Concat(Parameters.LoadingPlaceholderPath, KeyDownsamplingOnly);

                var vectLo = Parameters.CustomLoadingPlaceholderDataResolver as IVectorDataResolver;
                if (vectLo != null)
                {
                    if (vectLo.ReplaceStringMap == null || vectLo.ReplaceStringMap.Count == 0)
                        KeyForLoadingPlaceholder = string.Format("{0};(size={1}x{2},dip={3})", KeyForLoadingPlaceholder, vectLo.VectorWidth, vectLo.VectorHeight, vectLo.UseDipUnits);
                    else
                        KeyForLoadingPlaceholder = string.Format("{0};(size={1}x{2},dip={3},replace=({4}))", KeyForLoadingPlaceholder, vectLo.VectorWidth, vectLo.VectorHeight, vectLo.UseDipUnits,
                                               string.Join(",", vectLo.ReplaceStringMap.Select(x => string.Format("{0}/{1}", x.Key, x.Value)).OrderBy(v => v)));
                }
            }

            if (!string.IsNullOrWhiteSpace(Parameters.ErrorPlaceholderPath))
            {
                if (TransformPlaceholders)
                    KeyForErrorPlaceholder = string.Concat(Parameters.ErrorPlaceholderPath, KeyDownsamplingOnly, KeyTransformationsOnly);
                else
                    KeyForErrorPlaceholder = string.Concat(Parameters.ErrorPlaceholderPath, KeyDownsamplingOnly);

                var vectEr = Parameters.CustomLoadingPlaceholderDataResolver as IVectorDataResolver;
                if (vectEr != null)
                {
                    if (vectEr.ReplaceStringMap == null || vectEr.ReplaceStringMap.Count == 0)
                        KeyForErrorPlaceholder = string.Format("{0};(size={1}x{2},dip={3})", KeyForErrorPlaceholder, vectEr.VectorWidth, vectEr.VectorHeight, vectEr.UseDipUnits);
                    else
                        KeyForErrorPlaceholder = string.Format("{0};(size={1}x{2},dip={3},replace=({4}))", KeyForErrorPlaceholder, vectEr.VectorWidth, vectEr.VectorHeight, vectEr.UseDipUnits,
                                               string.Join(",", vectEr.ReplaceStringMap.Select(x => string.Format("{0}/{1}", x.Key, x.Value)).OrderBy(v => v)));
                }
            }

            ImageInformation.SetKey(Key, Parameters.CustomCacheKey);
            ImageInformation.SetPath(Parameters.Path);
        }

        public Configuration Configuration { get; private set; }

        public ImageInformation ImageInformation { get; private set; }

        public DownloadInformation DownloadInformation { get; private set; }

        public ITarget<TImageContainer, TImageView> PlatformTarget { get; private set; }

        public ITarget Target { get { return PlatformTarget as ITarget; } }

        protected IImageService ImageService { get; private set; }

        protected IMemoryCache<TImageContainer> MemoryCache { get; private set; }

        protected IDataResolverFactory DataResolverFactory { get; private set; }

        protected IDiskCache DiskCache { get { return Configuration.DiskCache; } }

        protected IDownloadCache DownloadCache { get { return Configuration.DownloadCache; } }

        protected IMiniLogger Logger { get { return Configuration.Logger; } }

        protected CancellationTokenSource CancellationTokenSource { get; private set; }

        protected IMainThreadDispatcher MainThreadDispatcher { get { return Configuration.MainThreadDispatcher; } }

        protected abstract int DpiToPixels(int size);

        protected abstract IDecoder<TDecoderContainer> ResolveDecoder(ImageInformation.ImageType type);

        protected abstract Task<TDecoderContainer> TransformAsync(TDecoderContainer bitmap, IList<ITransformation> transformations, string path, ImageSource source, bool isPlaceholder);

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

        public bool IsCompleted { get; private set; }

        public string Key { get; private set; }

        public string KeyRaw { get; private set; }

        public string KeyWithoutTransformations { get; private set; }

        protected string KeyTransformationsOnly { get; private set; }

        protected string KeyDownsamplingOnly { get; private set; }

        protected string KeyForLoadingPlaceholder { get; private set; }

        protected string KeyForErrorPlaceholder { get; private set; }

        protected WeakReference<TImageContainer> PlaceholderWeakReference { get; private set; }

        protected bool TransformPlaceholders
        {
            get
            {
                return (Parameters.TransformPlaceholdersEnabled.HasValue && Parameters.TransformPlaceholdersEnabled.Value)
                    || (!Parameters.TransformPlaceholdersEnabled.HasValue && Configuration.TransformPlaceholders);
            }
        }

        public TaskParameter Parameters { get; private set; }

        protected void ThrowIfCancellationRequested()
        {
            try
            {
                CancellationTokenSource?.Token.ThrowIfCancellationRequested();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void Cancel()
        {
            if (!_isDisposed)
            {
                if (IsCancelled || IsCompleted)
                    return;

                ImageService.RemovePendingTask(this);

                try
                {
                    CancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }

                if (Configuration.VerboseLoadingCancelledLogging)
                    Logger.Debug(string.Format("Image loading cancelled: {0}", Key));
            }
        }

        protected abstract Task<TImageContainer> GenerateImageFromDecoderContainerAsync(IDecodedImage<TDecoderContainer> decoded, ImageInformation imageInformation, bool isPlaceholder);

        protected abstract Task SetTargetAsync(TImageContainer image, bool animated);

        protected virtual void BeforeLoading(TImageContainer image, bool fromMemoryCache) { }

        protected virtual void AfterLoading(TImageContainer image, bool fromMemoryCache) { }

        async Task<TImageContainer> GenerateImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
        {
            using (imageData)
            {
                var decoder = ResolveDecoder(imageInformation.Type);
                var decoderContainer = await decoder.DecodeAsync(imageData, path, source, imageInformation, Parameters);

                if (enableTransformations && Parameters.Transformations != null && Parameters.Transformations.Count > 0)
                {
                    var transformations = Parameters.Transformations.ToList();

                    if (decoderContainer.IsAnimated)
                    {
                        for (int i = 0; i < decoderContainer.AnimatedImages.Length; i++)
                        {
                            decoderContainer.AnimatedImages[i].Image = await TransformAsync(decoderContainer.AnimatedImages[i].Image, transformations, path, source, isPlaceholder);
                        }
                    }
                    else
                    {
                        decoderContainer.Image = await TransformAsync(decoderContainer.Image, transformations, path, source, isPlaceholder);
                    }
                }

                return await GenerateImageFromDecoderContainerAsync(decoderContainer, imageInformation, isPlaceholder);
            }
        }

        public async virtual Task<bool> TryLoadFromMemoryCacheAsync()
        {
            try
            {
                if (Parameters.Preload && Parameters.CacheType.HasValue && 
                    (Parameters.CacheType.Value == CacheType.Disk || Parameters.CacheType.Value == CacheType.None))
                    return false;

                ThrowIfCancellationRequested();

                bool isFadeAnimationEnabledForCached = Parameters.FadeAnimationForCachedImagesEnabled.HasValue ? Parameters.FadeAnimationForCachedImagesEnabled.Value : Configuration.FadeAnimationForCachedImages;
                var result = await TryLoadFromMemoryCacheAsync(Key, true, isFadeAnimationEnabledForCached, false).ConfigureAwait(false);

                if (result)
                {
                    Logger.Debug(string.Format("Image loaded from cache: {0}", Key));
                    IsCompleted = true;

                    if (Configuration.ExecuteCallbacksOnUIThread && (Parameters?.OnSuccess != null || Parameters?.OnFinish != null))
                    {
                        await MainThreadDispatcher.PostAsync(() =>
                        {
                            Parameters?.OnSuccess?.Invoke(ImageInformation, LoadingResult.MemoryCache);
                            Parameters?.OnFinish?.Invoke(this);
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        Parameters?.OnSuccess?.Invoke(ImageInformation, LoadingResult.MemoryCache);
                        Parameters?.OnFinish?.Invoke(this);
                    }
                }
                else
                {
                    ThrowIfCancellationRequested();
                    // Loading placeholder if enabled
                    if (!_isLoadingPlaceholderLoaded && !string.IsNullOrWhiteSpace(Parameters.LoadingPlaceholderPath))
                    {
                        await ShowPlaceholder(Parameters.LoadingPlaceholderPath, KeyForLoadingPlaceholder,
                                              Parameters.LoadingPlaceholderSource, true).ConfigureAwait(false);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                if (Configuration.ClearMemoryCacheOnOutOfMemory && ex is OutOfMemoryException)
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

                    if (Configuration.ExecuteCallbacksOnUIThread && Parameters?.OnError != null)
                    {
                        await MainThreadDispatcher.PostAsync(() =>
                        {
                            Parameters?.OnError?.Invoke(ex);
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        Parameters?.OnError?.Invoke(ex);
                    }
                }
            }

            return false;
        }

        async Task<bool> TryLoadFromMemoryCacheAsync(string key, bool updateImageInformation, bool animated, bool isLoadingPlaceholder)
        {
            var found = MemoryCache.Get(key);
            if (found?.Item1 != null)
            {
                try
                {
                    BeforeLoading(found.Item1, true);

                    if (isLoadingPlaceholder)
                        PlaceholderWeakReference = new WeakReference<TImageContainer>(found.Item1);

                    ThrowIfCancellationRequested();

                    if (Target != null)
                        await SetTargetAsync(found.Item1, animated).ConfigureAwait(false);

                    if (updateImageInformation)
                        ImageInformation = found.Item2;
                }
                finally
                {
                    AfterLoading(found.Item1, true);
                }

                return true;
            }

            return false;
        }

        protected virtual async Task ShowPlaceholder(string path, string key, ImageSource source, bool isLoadingPlaceholder)
        {
            if (Parameters.Preload)
                return;

            if (!await TryLoadFromMemoryCacheAsync(key, false, false, isLoadingPlaceholder).ConfigureAwait(false))
            {
                try
                {
                    var customResolver = isLoadingPlaceholder ? Parameters.CustomLoadingPlaceholderDataResolver : Parameters.CustomErrorPlaceholderDataResolver;
                    var loadResolver = customResolver ?? DataResolverFactory.GetResolver(path, source, Parameters, Configuration);
                    loadResolver = new WrappedDataResolver(loadResolver);
                    DataResolverResult loadImageData;
                    TImageContainer loadImage;

                    if (!await _placeholdersResolveLock.WaitAsync(TimeSpan.FromSeconds(10), CancellationTokenSource.Token).ConfigureAwait(false))
                        return;

                    try
                    {                        
                        ThrowIfCancellationRequested();

                        if (await TryLoadFromMemoryCacheAsync(key, false, false, isLoadingPlaceholder).ConfigureAwait(false))
                        {
                            if (isLoadingPlaceholder)
                            {
                                _isLoadingPlaceholderLoaded = true;
                                Parameters.OnLoadingPlaceholderSet?.Invoke();
                            }
                            
                            return;
                        }

                        ThrowIfCancellationRequested();
                        loadImageData = await loadResolver.Resolve(path, Parameters, CancellationTokenSource.Token).ConfigureAwait(false);
                        ThrowIfCancellationRequested();

                        if (loadImageData.Stream != null)
                        {
                            using (loadImageData.Stream)
                            {
                                loadImage = await GenerateImageAsync(path, source, loadImageData.Stream, loadImageData.ImageInformation, TransformPlaceholders, true).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            loadImage = loadImageData.ImageContainer as TImageContainer;
                        }

                        if (loadImage != default(TImageContainer))
                            MemoryCache.Add(key, loadImageData.ImageInformation, loadImage);
                    }
                    finally
                    {
                        _placeholdersResolveLock.Release();
                    }

                    ThrowIfCancellationRequested();

                    if (isLoadingPlaceholder)
                        PlaceholderWeakReference = new WeakReference<TImageContainer>(loadImage);

                    if (Target != null)
                        await SetTargetAsync(loadImage, false).ConfigureAwait(false);

                    if (isLoadingPlaceholder)
                    {
                        _isLoadingPlaceholderLoaded = true;
                        Parameters.OnLoadingPlaceholderSet?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        throw;

                    Logger.Error("Setting placeholder failed", ex);
                }
            }
            else if (isLoadingPlaceholder)
            {
                _isLoadingPlaceholderLoaded = true;
                Parameters.OnLoadingPlaceholderSet?.Invoke();
            }
        }

        public async Task RunAsync()
        {
            LoadingResult loadingResult = LoadingResult.Failed;
            bool success = false;

            try
            {
                // LOAD IMAGE
                if (!(await TryLoadFromMemoryCacheAsync().ConfigureAwait(false)))
                {
                    if (Parameters.DelayInMs.HasValue && Parameters.DelayInMs.Value > 0)
                    {
                        await Task.Delay(Parameters.DelayInMs.Value).ConfigureAwait(false);
                    }
                    else if (!Parameters.Preload && Configuration.DelayInMs > 0)
                    {
                        await Task.Delay(Configuration.DelayInMs).ConfigureAwait(false);
                    }

                    Logger.Debug(string.Format("Generating/retrieving image: {0}", Key));
                    var resolver = Parameters.CustomDataResolver ?? DataResolverFactory.GetResolver(Parameters.Path, Parameters.Source, Parameters, Configuration);
                    resolver = new WrappedDataResolver(resolver);
                    var imageData = await resolver.Resolve(Parameters.Path, Parameters, CancellationTokenSource.Token).ConfigureAwait(false);
                    loadingResult = imageData.LoadingResult;

                    ImageInformation = imageData.ImageInformation;
                    ImageInformation.SetKey(Key, Parameters.CustomCacheKey);
                    ImageInformation.SetPath(Parameters.Path);
                    ThrowIfCancellationRequested();

                    // Preload
                    if (Parameters.Preload && Parameters.CacheType.HasValue && Parameters.CacheType.Value == CacheType.Disk)
                    {
                        if (loadingResult == LoadingResult.Internet)
                            Logger?.Debug(string.Format("DownloadOnly success: {0}", Key));

                        success = true;

                        return;
                    }

                    ThrowIfCancellationRequested();

                    TImageContainer image;

                    if (imageData.Stream != null)
                    {
                        using (imageData.Stream)
                        {
                            image = await GenerateImageAsync(Parameters.Path, Parameters.Source, imageData.Stream, imageData.ImageInformation, true, false).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        image = imageData.ImageContainer as TImageContainer;
                    }

                    ThrowIfCancellationRequested();

                    try
                    {
                        BeforeLoading(image, false);

                        if (image != default(TImageContainer) && CanUseMemoryCache)
                            MemoryCache.Add(Key, imageData.ImageInformation, image);

                        ThrowIfCancellationRequested();

                        bool isFadeAnimationEnabled = Parameters.FadeAnimationEnabled ?? Configuration.FadeAnimationEnabled;

                        if (Target != null)
                            await SetTargetAsync(image, isFadeAnimationEnabled).ConfigureAwait(false);
                    }
                    finally
                    {
                        AfterLoading(image, false);
                    }
                }

                success = true;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException || ex is ObjectDisposedException)
                {
                    if (Configuration.VerboseLoadingCancelledLogging)
                    {
                        Logger.Debug(string.Format("Image loading cancelled: {0}", Key));
                    }
                }
                else
                {
                    if (Configuration.ClearMemoryCacheOnOutOfMemory && ex is OutOfMemoryException)
                    {
                        MemoryCache.Clear();
                    }

                    Logger.Error(string.Format("Image loading failed: {0}", Key), ex);

                    if (Configuration.ExecuteCallbacksOnUIThread && Parameters?.OnError != null)
                    {
                        await MainThreadDispatcher.PostAsync(() =>
                        {
                            Parameters?.OnError?.Invoke(ex);
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        Parameters?.OnError?.Invoke(ex);
                    }

                    try
                    {
                        // Error placeholder if enabled
                        if (!Parameters.Preload && !string.IsNullOrWhiteSpace(Parameters.ErrorPlaceholderPath))
                        {
                            await ShowPlaceholder(Parameters.ErrorPlaceholderPath, KeyForErrorPlaceholder,
                                                  Parameters.ErrorPlaceholderSource, false).ConfigureAwait(false);
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
                try
                {
                    if (CancellationTokenSource?.IsCancellationRequested == false)
                        CancellationTokenSource.Cancel();
                }
                catch (Exception)
                {
                }

                IsCompleted = true;

                using (Parameters)
                {
                    if (Configuration.ExecuteCallbacksOnUIThread && Parameters?.OnFinish != null)
                    {
                        await MainThreadDispatcher.PostAsync(() =>
                        {
                            if (success)
                                Parameters?.OnSuccess?.Invoke(ImageInformation, loadingResult);
                            Parameters?.OnFinish?.Invoke(this);
                        }).ConfigureAwait(false);
                    }
                    else
                    {
                        if (success)
                            Parameters?.OnSuccess?.Invoke(ImageInformation, loadingResult);
                        Parameters?.OnFinish?.Invoke(this);
                    }

                    ImageService.RemovePendingTask(this);
                }
            }
        }

        bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Parameters.TryDispose();
                CancellationTokenSource.TryDispose();

                _isDisposed = true;
            }
        }
    }
}
