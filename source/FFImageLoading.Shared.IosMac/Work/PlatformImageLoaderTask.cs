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
using ImageIO;
using System.Collections.Generic;
using FFImageLoading.Decoders;
using CoreGraphics;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif

namespace FFImageLoading.Work
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<PImage, PImage, TImageView> where TImageView : class
    {
        static readonly SemaphoreSlim _decodingLock = new SemaphoreSlim(2, 2);
        static IDecoder<PImage> _webpDecoder = new WebPDecoder();

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

        protected override int DpiToPixels(int size)
        {
            return size.DpToPixels();
        }

        protected override IDecoder<PImage> ResolveDecoder(ImageInformation.ImageType type)
        {
            switch (type)
            {
                case ImageInformation.ImageType.GIF:
                    return new GifDecoder();

                case ImageInformation.ImageType.WEBP:
                    return _webpDecoder;

                default:
                    return new BaseDecoder();
            }
        }

        protected override async Task<PImage> TransformAsync(PImage bitmap, IList<ITransformation> transformations, string path, ImageSource source, bool isPlaceholder)
        {
            await _decodingLock.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false); // Applying transformations is both CPU and memory intensive
            ThrowIfCancellationRequested();

            try
            {
                foreach (var transformation in transformations)
                {
                    ThrowIfCancellationRequested();

                    var old = bitmap;

                    try
                    {
                        var bitmapHolder = transformation.Transform(new BitmapHolder(bitmap), path, source, isPlaceholder, Key);
                        bitmap = bitmapHolder.ToNative();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(string.Format("Transformation failed: {0}", transformation.Key), ex);
                        throw;
                    }
                    finally
                    {
                        // Transformation succeeded, so garbage the source
                        if (old != null && old.Handle != IntPtr.Zero && old != bitmap && old.Handle != bitmap.Handle)
                        {
                            old.TryDispose();
                        }
                    }
                }
            }
            finally
            {
                _decodingLock.Release();
            }

            return bitmap;
        }

        protected override Task<PImage> GenerateImageFromDecoderContainerAsync(IDecodedImage<PImage> decoded, ImageInformation imageInformation, bool isPlaceholder)
        {
            PImage result;

            if (decoded.IsAnimated)
            {
#if __IOS__
                result = PImage.CreateAnimatedImage(decoded.AnimatedImages
                                                    .Select(v => v.Image)
                                                    .Where(v => v?.CGImage != null).ToArray(), decoded.AnimatedImages.Sum(v => v.Delay) / 100.0);
#elif __MACOS__
                using (var mutableData = NSMutableData.Create())
                {
                    var fileOptions = new CGImageDestinationOptions();
                    fileOptions.GifDictionary = new NSMutableDictionary();
                    fileOptions.GifDictionary[ImageIO.CGImageProperties.GIFLoopCount] = new NSString("0");
                    //options.GifDictionary[ImageIO.CGImageProperties.GIFHasGlobalColorMap] = new NSString("true");

                    using (var destintation = CGImageDestination.Create(mutableData, MobileCoreServices.UTType.GIF, decoded.AnimatedImages.Length, fileOptions))
                    {
                        for (int i = 0; i < decoded.AnimatedImages.Length; i++)
                        {
                            var options = new CGImageDestinationOptions();
                            options.GifDictionary = new NSMutableDictionary();
                            options.GifDictionary[ImageIO.CGImageProperties.GIFUnclampedDelayTime] = new NSString(decoded.AnimatedImages[i].Delay.ToString());
                            destintation.AddImage(decoded.AnimatedImages[i].Image.CGImage, options);
                        }

                        destintation.Close();
                    }

                    result = new PImage(mutableData);

                    // TODO I really don't know why representations count is 1, anyone?
                    // var test = result.Representations();
                }
#endif                
            }
            else
            {
                result = decoded.Image;
            }

            return Task.FromResult(result);
        }
    }
}

