using System;
using FFImageLoading.Helpers;
using System.Threading.Tasks;
using Foundation;
using System.Linq;
using System.IO;
using FFImageLoading.Extensions;
using System.Threading;
using FFImageLoading.Config;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using ImageIO;
using System.Collections.Generic;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif

namespace FFImageLoading.Work
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<PImage, TImageView> where TImageView : class
    {
        static readonly SemaphoreSlim _decodingLock = new SemaphoreSlim(2, 2);
        static readonly SemaphoreSlim _webpLock = new SemaphoreSlim(1, 1);
        static IImageFileDecoder<PImage> _webpDecoder = new WebPDecoder();

        public PlatformImageLoaderTask(ITarget<PImage, TImageView> target, TaskParameter parameters, IImageService imageService) : base(ImageCache.Instance, target, parameters, imageService)
        {
            // do not remove! Kicks scale retrieval so it's available for all, without deadlocks due to accessing MainThread
            ScaleHelper.Init();
        }

        protected override Task SetTargetAsync(PImage image, bool animated)
        {
            if (Target == null)
                return Task.FromResult(true);
            
            return MainThreadDispatcher.PostAsync(() =>
            {
                ThrowIfCancellationRequested();
                PlatformTarget.Set(this, image, animated);
            });
        }

        protected async override Task<PImage> GenerateImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
        {
            PImage imageIn = null;

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

                // Special case to handle WebP decoding on iOS
                if (source != ImageSource.Stream && imageInformation.Type == ImageInformation.ImageType.WEBP)
                {
                    await _webpLock.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                    ThrowIfCancellationRequested();
                    try
                    {
                        var decodedWebP = _webpDecoder.Decode(imageData);
                        //TODO Add WebP images downsampling!
                        imageIn = decodedWebP;
                    }
                    finally
                    {
                        _webpLock.Release();
                    }
                }
                else
                {
                    var nsdata = NSData.FromStream(imageData);
                    imageIn = nsdata.ToImage(new CoreGraphics.CGSize(downsampleWidth, downsampleHeight), ScaleHelper.Scale, Configuration, Parameters, NSDataExtensions.RCTResizeMode.ScaleAspectFill, imageInformation, allowUpscale);
                }
            }
            finally
            {
                imageData.TryDispose();
            }

            ThrowIfCancellationRequested();

            if (enableTransformations && Parameters.Transformations != null && Parameters.Transformations.Count > 0)
            {
                var transformations = Parameters.Transformations.ToList();

                await _decodingLock.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false); // Applying transformations is both CPU and memory intensive
                ThrowIfCancellationRequested();

                try
                {
                    bool isAnimation = false;
#if __IOS__
                    if (imageIn.Images != null) isAnimation = true;
#endif
                    if (!isAnimation)
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
                                    old.TryDispose();
                            }
                        }
                    }
                    else
                    {
                        // no animted image support for mac implemented
#if __IOS__
                        var animatedImages = imageIn.Images.ToArray();

                        for (int i = 0; i < animatedImages.Length; i++)
                        {
                            var tempImage = animatedImages[i];

                            if (tempImage.CGImage == null)
                                continue;

                            foreach (var transformation in transformations)
                            {
                                ThrowIfCancellationRequested();

                                var old = tempImage;

                                try
                                {
                                    var bitmapHolder = transformation.Transform(new BitmapHolder(tempImage), path, source, isPlaceholder, Key);
                                    tempImage = bitmapHolder.ToNative();
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(string.Format("Transformation failed: {0}", transformation.Key), ex);
                                    throw;
                                }
                                finally
                                {
                                    if (old != null && old != tempImage && old.Handle != tempImage.Handle)
                                    old.TryDispose();
                                }
                            }

                            animatedImages[i] = tempImage;
                        }

                        var oldImageIn = imageIn;
                        imageIn = PImage.CreateAnimatedImage(animatedImages.Where(v => v.CGImage != null).ToArray(), imageIn.Duration);
                        oldImageIn.TryDispose();
#endif
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

