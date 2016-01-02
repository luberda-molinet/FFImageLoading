using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Extensions;
using FFImageLoading.DataResolver;

#if SILVERLIGHT
using System.Windows.Controls;
using System.Windows.Media.Imaging;
#else
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
#endif

namespace FFImageLoading.Work
{
    public class ImageLoaderTask : ImageLoaderTaskBase
    {
        private readonly Func<Image> _getNativeControl;
        private readonly Action<WriteableBitmap, bool> _doWithImage;

        public ImageLoaderTask(IDownloadCache downloadCache, IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, TaskParameter parameters, Func<Image> getNativeControl, Action<WriteableBitmap, bool> doWithImage)
            : base(mainThreadDispatcher, miniLogger, parameters, false)
        {
            _getNativeControl = getNativeControl;
            _doWithImage = doWithImage;

            DownloadCache = downloadCache;
        }

        protected IDownloadCache DownloadCache { get; private set; }

        public override bool UsesSameNativeControl(IImageLoaderTask task)
        {
            var loaderTask = task as ImageLoaderTask;

            if (loaderTask == null)
                return false;

            return UsesSameNativeControl(loaderTask);
        }

        private bool UsesSameNativeControl(ImageLoaderTask task)
        {
            var currentControl = _getNativeControl();
            var control = task._getNativeControl();

            if (currentControl == null || control == null)
                return false;

            return currentControl == control;
        }

        public override async Task<bool> PrepareAndTryLoadingFromCacheAsync()
        {
            var cacheResult = await TryLoadingFromCacheAsync().ConfigureAwait(false);
            if (cacheResult == CacheResult.Found || cacheResult == CacheResult.ErrorOccured) // If image is loaded from cache there is nothing to do here anymore, if something weird happened with the cache... error callback has already been called, let's just leave
                return true; // stop processing if loaded from cache OR if loading from cached raised an exception

            await LoadPlaceHolderAsync(Parameters.LoadingPlaceholderPath, Parameters.LoadingPlaceholderSource).ConfigureAwait(false);
            return false;
        }

        protected override async Task<GenerateResult> TryGeneratingImageAsync()
        {
            WithLoadingResult<WriteableBitmap> imageWithResult = null;
            WriteableBitmap image = null;

            try
            {
                imageWithResult = await RetrieveImageAsync(Parameters.Path, Parameters.Source, false).ConfigureAwait(false);
                image = imageWithResult == null ? null : imageWithResult.Item;
            }
            catch (Exception ex)
            {
                Logger.Error("An error occured while retrieving image.", ex);
                image = null;
            }

            if (image == null)
            {
                await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
                return GenerateResult.Failed;
            }

            if (CancellationToken.IsCancellationRequested)
                return GenerateResult.Canceled;

            if (_getNativeControl() == null)
                return GenerateResult.InvalidTarget;

            try
            {
                // Post on main thread
                await MainThreadDispatcher.PostAsync(() =>
                {
                    if (CancellationToken.IsCancellationRequested)
                        return;

                    _doWithImage(image, false);
                    Completed = true;
                    Parameters.OnSuccess(new ImageSize(image.PixelWidth, image.PixelHeight), imageWithResult.Result);
                }).ConfigureAwait(false);

                if (!Completed)
                    return GenerateResult.Failed;
            }
            catch (Exception ex2)
            {
                await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
                throw ex2;
            }

            return GenerateResult.Success;
        }

        public override async Task<CacheResult> TryLoadingFromCacheAsync()
        {
            try
            {
                var nativeControl = _getNativeControl();
                if (nativeControl == null)
                    return CacheResult.NotFound; // weird situation, dunno what to do

                var value = ImageCache.Instance.Get(GetKey());
                if (value == null)
                    return CacheResult.NotFound; // not available in the cache

                if (IsCancelled)
                    return CacheResult.NotFound; // not sure what to return in that case

                int pixelWidth = 0;
                int pixelHeight = 0;

                await MainThreadDispatcher.PostAsync(() =>
                {
                    _doWithImage(value, true);
                    pixelWidth = value.PixelWidth;
                    pixelHeight = value.PixelHeight;
                }).ConfigureAwait(false);

                if (IsCancelled)
                    return CacheResult.NotFound; // not sure what to return in that case

                Completed = true;

                if (Parameters.OnSuccess != null)
                    Parameters.OnSuccess(new ImageSize(pixelWidth, pixelHeight), LoadingResult.MemoryCache);

                return CacheResult.Found; // found and loaded from cache
            }
            catch (Exception ex)
            {
                Parameters.OnError(ex);
                return CacheResult.ErrorOccured; // weird, what can we do if loading from cache fails
            }
        }

        public override async Task<GenerateResult> LoadFromStreamAsync(Stream stream, bool isPlaceholder)
        {
            if (stream == null)
                return GenerateResult.Failed;

            if (CancellationToken.IsCancellationRequested)
                return GenerateResult.Canceled;

            WithLoadingResult<WriteableBitmap> imageWithResult = null;
            WriteableBitmap image = null;
            try
            {
                imageWithResult = await GetImageAsync("Stream", ImageSource.Stream, isPlaceholder, stream).ConfigureAwait(false);
                image = imageWithResult == null ? null : imageWithResult.Item;
            }
            catch (Exception ex)
            {
                Logger.Error("An error occured while retrieving image.", ex);
                image = null;
            }

            if (image == null)
            {
                await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
                return GenerateResult.Failed;
            }

            if (CancellationToken.IsCancellationRequested)
                return GenerateResult.Canceled;

            if (_getNativeControl() == null)
                return GenerateResult.InvalidTarget;

            try
            {
                int pixelWidth = 0;
                int pixelHeight = 0;

                // Post on main thread
                await MainThreadDispatcher.PostAsync(() =>
                {
                    if (CancellationToken.IsCancellationRequested)
                        return;

                    _doWithImage(image, false);
                    pixelWidth = image.PixelWidth;
                    pixelHeight = image.PixelHeight;
                    Completed = true;
                    Parameters.OnSuccess(new ImageSize(pixelWidth, pixelHeight), imageWithResult.Result);
                }).ConfigureAwait(false);

                if (!Completed)
                    return GenerateResult.Failed;
            }
            catch (Exception ex2)
            {
                await LoadPlaceHolderAsync(Parameters.ErrorPlaceholderPath, Parameters.ErrorPlaceholderSource).ConfigureAwait(false);
                throw ex2;
            }

            return GenerateResult.Success;
        }


        protected virtual async Task<WithLoadingResult<WriteableBitmap>> GetImageAsync(string sourcePath, ImageSource source,
            bool isPlaceholder, Stream originalStream = null)
        {
            if (CancellationToken.IsCancellationRequested)
                return null;

            byte[] bytes = null;
            string path = sourcePath;
            LoadingResult? result = null;

            try
            {
                if (originalStream != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        await originalStream.CopyToAsync(ms).ConfigureAwait(false);
                        bytes = ms.ToArray();
                        path = sourcePath;
                        result = LoadingResult.Stream;
                    }
                }
                else
                {
                    using (var resolver = DataResolverFactory.GetResolver(source, Parameters, DownloadCache))
                    {
                        var data = await resolver.GetData(path, CancellationToken.Token).ConfigureAwait(false);
                        if (data == null)
                            return null;

                        bytes = data.Data;
                        path = data.ResultIdentifier;
                        result = data.Result;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Debug(string.Format("Image request for {0} got cancelled.", path));
                return null;
            }
            catch (Exception ex)
            {
                var message = String.Format("Unable to retrieve image data from source: {0}", sourcePath);
                Logger.Error(message, ex);
                Parameters.OnError(ex);
                return null;
            }

            if (bytes == null)
                return null;

            var image = await Task.Run(async () =>
            {
                if (CancellationToken.IsCancellationRequested)
                    return null;

                WriteableBitmap writableBitmap = null;

                // Special case to handle WebP decoding
                if (sourcePath.ToLowerInvariant().EndsWith(".webp"))
                {
                    //TODO 
                    throw new NotImplementedException("Webp is not implemented on Windows");
                }

                bool transformPlaceholdersEnabled = Parameters.TransformPlaceholdersEnabled.HasValue ?
                    Parameters.TransformPlaceholdersEnabled.Value : ImageService.Config.TransformPlaceholders;

                if (Parameters.Transformations != null && Parameters.Transformations.Count > 0
                && (!isPlaceholder || (isPlaceholder && transformPlaceholdersEnabled)))
                {
                    BitmapHolder imageIn = await bytes.ToBitmapHolderAsync(Parameters.DownSampleSize, Parameters.DownSampleInterpolationMode).ConfigureAwait(false);

                    foreach (var transformation in Parameters.Transformations.ToList() /* to prevent concurrency issues */)
                    {
                        if (CancellationToken.IsCancellationRequested)
                            return null;

                        try
                        {
                            var old = imageIn;

                            IBitmap bitmapHolder = transformation.Transform(imageIn);
                            imageIn = bitmapHolder.ToNative();

							if (old != null && old != imageIn && old.Pixels != imageIn.Pixels)
							{
								old.FreePixels();
								old = null;
							}
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Can't apply transformation " + transformation.Key + " to image " + path, ex);
                        }
                    }

                    writableBitmap = await imageIn.ToBitmapImageAsync();
                    imageIn.FreePixels();
                    imageIn = null;
                }
                else
                {
                    writableBitmap = await bytes.ToBitmapImageAsync(Parameters.DownSampleSize, Parameters.DownSampleInterpolationMode);
                }

                bytes = null;

                return writableBitmap;
            }).ConfigureAwait(false);

            return WithLoadingResult.Encapsulate(image, result.Value);
        }
        
        private async Task<bool> LoadPlaceHolderAsync(string placeholderPath, ImageSource source)
        {
            if (string.IsNullOrWhiteSpace(placeholderPath))
                return false;

            WriteableBitmap image = ImageCache.Instance.Get(GetKey(placeholderPath));

            if (image == null)
            {
                try
                {
                    var imageWithResult = await RetrieveImageAsync(placeholderPath, source, true).ConfigureAwait(false);
                    image = imageWithResult == null ? null : imageWithResult.Item;
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occured while retrieving placeholder's drawable.", ex);
                    return false;
                }
            }

            if (image == null)
                return false;

            var view = _getNativeControl();
            if (view == null)
                return false;

            if (CancellationToken.IsCancellationRequested)
                return false;

            // Post on main thread but don't wait for it
            MainThreadDispatcher.Post(() =>
            {
                if (CancellationToken.IsCancellationRequested)
                    return;

                _doWithImage(image, false);
            });

            return true;
        }

        private async Task<WithLoadingResult<WriteableBitmap>> RetrieveImageAsync(string sourcePath, ImageSource source, bool isPlaceholder)
        {
            // If the image cache is available and this task has not been cancelled by another
            // thread and the ImageView that was originally bound to this task is still bound back
            // to this task and our "exit early" flag is not set then try and fetch the bitmap from
            // the cache
            if (CancellationToken.IsCancellationRequested || _getNativeControl() == null || ImageService.ExitTasksEarly)
                return null;

            var imageWithResult = await GetImageAsync(sourcePath, source, isPlaceholder).ConfigureAwait(false);

            if (imageWithResult == null || imageWithResult.Item == null)
                return null;

            // FMT: even if it was canceled, if we have the bitmap we add it to the cache
            ImageCache.Instance.Add(GetKey(sourcePath), imageWithResult.Item);

            return imageWithResult;
        }
    }
}