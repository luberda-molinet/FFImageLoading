using System;
using FFImageLoading.Helpers;
using UIKit;
using System.Threading.Tasks;
using Foundation;
using System.Linq;
using System.IO;
using FFImageLoading.Extensions;
using System.Threading;
using FFImageLoading.Config;
using FFImageLoading.Cache;

namespace FFImageLoading.Work
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<UIImage, TImageView> where TImageView : class
    {
        static readonly SemaphoreSlim _decodingLock = new SemaphoreSlim(1, 1);

        public PlatformImageLoaderTask(ITarget<UIImage, TImageView> target, TaskParameter parameters, IImageService imageService, Configuration configuration, IMainThreadDispatcher mainThreadDispatcher)
            : base(ImageCache.Instance, configuration.DataResolverFactory ?? DataResolvers.DataResolverFactory.Instance, target, parameters, imageService, configuration, mainThreadDispatcher, true)
        {
            // do not remove! Kicks scale retrieval so it's available for all, without deadlocks due to accessing MainThread
            ScaleHelper.Init();
        }

        protected override Task SetTargetAsync(UIImage image, bool animated)
        {
            return MainThreadDispatcher.PostAsync(() =>
            {
                ThrowIfCancellationRequested();
                PlatformTarget.Set(this, image, animated);
            });
        }

        protected async override Task<UIImage> GenerateImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
        {
            UIImage imageIn = null;

            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            ThrowIfCancellationRequested();

            try
            {
                // Special case to handle WebP decoding on iOS

                string ext = null;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    if (source == ImageSource.Url && Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute))
                        ext = Path.GetExtension(new Uri(path).LocalPath).ToLowerInvariant();
                    else
                        ext = Path.GetExtension(path).ToLowerInvariant();
                }
                
                if (source != ImageSource.Stream && ext == ".webp")
                {
                    imageIn = new WebP.Touch.WebPCodec().Decode(imageData);
                }
                else
                {
                    var nsdata = NSData.FromStream(imageData);
                    int downsampleWidth = Parameters.DownSampleSize?.Item1 ?? 0;
                    int downsampleHeight = Parameters.DownSampleSize?.Item2 ?? 0;
                    bool allowUpscale = Parameters.AllowUpscale ?? Configuration.AllowUpscale;

                    if (Parameters.DownSampleUseDipUnits)
                    {
                        downsampleWidth = downsampleWidth.PointsToPixels();
                        downsampleHeight = downsampleHeight.PointsToPixels();
                    }

                    imageIn = nsdata.ToImage(new CoreGraphics.CGSize(downsampleWidth, downsampleHeight), ScaleHelper.Scale, NSDataExtensions.RCTResizeMode.ScaleAspectFill, imageInformation, allowUpscale);
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
                            var bitmapHolder = transformation.Transform(new BitmapHolder(imageIn), path, source, isPlaceholder, Key);
                            imageIn = bitmapHolder.ToNative();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(string.Format("Transformation failed: {0}", transformation.Key), ex);
                            throw;
                        }
                        finally
                        {
                            if (old != null && old != imageIn && old.Handle != imageIn.Handle)
                                old.Dispose();
                        }
                    }
                }
                finally
                {
                    _decodingLock.Release();
                }
            }

            return imageIn;
        }

        protected override int DpiToPixels(int size)
        {
            return size.PointsToPixels();
        }
    }
}

