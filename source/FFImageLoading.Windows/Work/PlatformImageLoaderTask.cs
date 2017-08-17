using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.Extensions;
using FFImageLoading.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Work
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<WriteableBitmap, TImageView> where TImageView : class
    {
        static readonly SemaphoreSlim _decodingLock = new SemaphoreSlim(1, 1);

        public PlatformImageLoaderTask(ITarget<WriteableBitmap, TImageView> target, TaskParameter parameters, IImageService imageService, Configuration configuration, IMainThreadDispatcher mainThreadDispatcher)
            : base(ImageCache.Instance, configuration.DataResolverFactory ?? DataResolvers.DataResolverFactory.Instance, target, parameters, imageService, configuration, mainThreadDispatcher, false)
        {

        }

        protected override Task SetTargetAsync(WriteableBitmap image, bool animated)
        {
            return MainThreadDispatcher.PostAsync(() =>
            {
                ThrowIfCancellationRequested();
                PlatformTarget.Set(this, image, animated);
            });
        }

        protected async override Task<WriteableBitmap> GenerateImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
        {
            BitmapHolder imageIn = null;

            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            ThrowIfCancellationRequested();

            try
            {
                // Special case to handle WebP decoding on Windows
                string ext = null;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    if (source == ImageSource.Url && Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute))
                        ext = Path.GetExtension(new Uri(path).LocalPath).ToLowerInvariant();
                    else
                        ext = Path.GetExtension(path).ToLowerInvariant();
                }
                
                bool allowUpscale = Parameters.AllowUpscale ?? Configuration.AllowUpscale;
                if (source != ImageSource.Stream && ext == ".webp")
                {
                    throw new NotImplementedException("Webp is not implemented on Windows");
                }
                else if (enableTransformations && Parameters.Transformations != null && Parameters.Transformations.Count > 0)
                {
                    imageIn = await imageData.ToBitmapHolderAsync(Parameters.DownSampleSize, Parameters.DownSampleUseDipUnits, Parameters.DownSampleInterpolationMode, allowUpscale, imageInformation).ConfigureAwait(false);
                }
                else
                {
                    return await imageData.ToBitmapImageAsync(Parameters.DownSampleSize, Parameters.DownSampleUseDipUnits, Parameters.DownSampleInterpolationMode, allowUpscale, imageInformation).ConfigureAwait(false);
                }
            }
            finally
            {
                imageData?.Dispose();
            }

            ThrowIfCancellationRequested();

            if (enableTransformations && Parameters.Transformations != null && Parameters.Transformations.Count > 0)
            {
                var transformations = Parameters.Transformations.ToList();

                await _decodingLock.WaitAsync().ConfigureAwait(false); // Applying transformations is both CPU and memory intensive

                try
                {
                    foreach (var transformation in transformations)
                    {
                        ThrowIfCancellationRequested();

                        var old = imageIn;

                        try
                        {
                            IBitmap bitmapHolder = transformation.Transform(imageIn, path, source, isPlaceholder, Key);
                            imageIn = bitmapHolder.ToNative();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(string.Format("Transformation failed: {0}", transformation.Key), ex);
                            throw;
                        }
                        finally
                        {
                            if (old != null && old != imageIn && old.PixelData != imageIn.PixelData)
                            {
                                old.FreePixels();
                                old = null;
                            }
                        }
                    }
                }
                finally
                {
                    _decodingLock.Release();
                }
            }

            try
            {
                return await imageIn.ToBitmapImageAsync();
            }
            finally
            {
                imageIn.FreePixels();
                imageIn = null;
            }
        }

        protected override int DpiToPixels(int size)
        {
            return size.PointsToPixels();
        }
    }
}
