using System;
using FFImageLoading.Helpers;
using AppKit;
using System.Threading.Tasks;
using Foundation;
using System.Linq;
using System.IO;
using FFImageLoading.Extensions;
using System.Threading;
using FFImageLoading.Config;
using FFImageLoading.Cache;
using ImageIO;
using System.Collections.Generic;

namespace FFImageLoading.Work
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<NSImage, TImageView> where TImageView : class
    {
        static readonly SemaphoreSlim _decodingLock = new SemaphoreSlim(1, 1);
        static readonly SemaphoreSlim _webpLock = new SemaphoreSlim(1, 1);
        static object _webpDecoder;

        public PlatformImageLoaderTask(ITarget<NSImage, TImageView> target, TaskParameter parameters, IImageService imageService, Configuration configuration, IMainThreadDispatcher mainThreadDispatcher)
            : base(ImageCache.Instance, configuration.DataResolverFactory ?? DataResolvers.DataResolverFactory.Instance, target, parameters, imageService, configuration, mainThreadDispatcher, true)
        {
            // do not remove! Kicks scale retrieval so it's available for all, without deadlocks due to accessing MainThread
            ScaleHelper.Init();
        }

        protected override Task SetTargetAsync(NSImage image, bool animated)
        {
            return MainThreadDispatcher.PostAsync(() =>
            {
                ThrowIfCancellationRequested();
                PlatformTarget.Set(this, image, animated);
            });
        }

        protected async override Task<NSImage> GenerateImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
        {
            NSImage imageIn = null;

            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            ThrowIfCancellationRequested();

            try
            {
                int downsampleWidth = Parameters.DownSampleSize?.Item1 ?? 0;
                int downsampleHeight = Parameters.DownSampleSize?.Item2 ?? 0;
                bool allowUpscale = Parameters.AllowUpscale ?? Configuration.AllowUpscale;

                if (Parameters.DownSampleUseDipUnits)
                {
                    downsampleWidth = downsampleWidth.PointsToPixels();
                    downsampleHeight = downsampleHeight.PointsToPixels();
                }

                var nsdata = NSData.FromStream(imageData);
                imageIn = nsdata.ToImage(new CoreGraphics.CGSize(downsampleWidth, downsampleHeight), ScaleHelper.Scale, Configuration, Parameters, NSDataExtensions.RCTResizeMode.ScaleAspectFill, imageInformation, allowUpscale);

            }
            finally
            {
                imageData?.Dispose();
            }

            ThrowIfCancellationRequested();

            if (enableTransformations && Parameters.Transformations != null && Parameters.Transformations.Count > 0)
            {
                var transformations = Parameters.Transformations.ToList();

                await _decodingLock.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false); // Applying transformations is both CPU and memory intensive
                ThrowIfCancellationRequested();

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

