using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.Drawables;
using FFImageLoading.Extensions;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using FFImageLoading.Views;
using FFImageLoading.Decoders;
using System.Collections.Generic;

namespace FFImageLoading
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<Bitmap, SelfDisposingBitmapDrawable, TImageView> where TImageView : class
    {
        static readonly SemaphoreSlim _decodingLock = new SemaphoreSlim(1, 1);
        GifDecoder _gifDecoder = new GifDecoder();

        public PlatformImageLoaderTask(ITarget<SelfDisposingBitmapDrawable, TImageView> target, TaskParameter parameters, IImageService imageService) : base(ImageCache.Instance, target, parameters, imageService)
        {
        }

        protected Context Context
        {
            get
            {
                return new ContextWrapper(Android.App.Application.Context);
            }
        }

        protected async override Task SetTargetAsync(SelfDisposingBitmapDrawable image, bool animated)
        {
            if (Target == null)
                return;

            ThrowIfCancellationRequested();

            var ffDrawable = image as FFBitmapDrawable;
            if (ffDrawable != null)
            {
                if (ffDrawable.IsAnimationRunning)
                {
                    var mut = new FFBitmapDrawable(Context.Resources, ffDrawable.Bitmap, ffDrawable);
                    ffDrawable = mut as FFBitmapDrawable;
                    image = ffDrawable;

                    // old hacky workaround
                    //await Task.Delay(ffDrawable.FadeDuration + 50).ConfigureAwait(false);
                }

                if (animated)
                {
                    SelfDisposingBitmapDrawable placeholderDrawable = null;
                    PlaceholderWeakReference?.TryGetTarget(out placeholderDrawable);

                    if (placeholderDrawable == null)
                    {
                        // Enable fade animation when no placeholder is set and the previous image is not null
                        var imageView = PlatformTarget.Control as ImageViewAsync;
                        placeholderDrawable = imageView?.Drawable as SelfDisposingBitmapDrawable;
                    }

                    if (placeholderDrawable.IsValidAndHasValidBitmap())
                    {
                        int fadeDuration = Parameters.FadeAnimationDuration.HasValue ?
                            Parameters.FadeAnimationDuration.Value : Configuration.FadeAnimationDuration;

                        placeholderDrawable?.SetIsRetained(true);
                        ffDrawable?.SetPlaceholder(placeholderDrawable, fadeDuration);
                        placeholderDrawable?.SetIsRetained(false);
                    }
                }
                else
                {
                    ffDrawable?.SetPlaceholder(null, 0);
                }
            }

            await MainThreadDispatcher.PostAsync(() =>
            {
                ThrowIfCancellationRequested();

                PlatformTarget.Set(this, image, animated);

            }).ConfigureAwait(false);
        }

        protected override void BeforeLoading(SelfDisposingBitmapDrawable image, bool fromMemoryCache)
        {
            base.BeforeLoading(image, fromMemoryCache);
            image?.SetIsRetained(true);
        }

        protected override void AfterLoading(SelfDisposingBitmapDrawable image, bool fromMemoryCache)
        {
            base.AfterLoading(image, fromMemoryCache);
            image?.SetIsRetained(false);
        }

        protected override int DpiToPixels(int size)
        {
            return size.DpToPixels();
        }

        protected override IDecoder<Bitmap> ResolveDecoder(ImageInformation.ImageType type)
        {
            switch (type)
            {
                case ImageInformation.ImageType.GIF:
                    return new GifDecoder();

                default:
                    return new BaseDecoder();
            }
        }

        protected override async Task<Bitmap> TransformAsync(Bitmap bitmap, IList<ITransformation> transformations, string path, ImageSource source, bool isPlaceholder)
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
                        if (old != null && old.Handle != IntPtr.Zero && !old.IsRecycled && old != bitmap && old.Handle != bitmap.Handle)
                        {
                            // Adding to pool gives us a better performance
                            // ImageCache.Instance.AddToReusableSet(new SelfDisposingBitmapDrawable(old) { InCacheKey = Guid.NewGuid().ToString() });
                            // Disabled as it caused OOM exceptions / extensive memory usage on older devices, in favor of this:
                            old?.Recycle();
                            old.TryDispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var javaException = ex as Java.Lang.Throwable;
                if (javaException != null && javaException.Class == Java.Lang.Class.FromType(typeof(Java.Lang.OutOfMemoryError)))
                {
                    throw new OutOfMemoryException();
                }

                throw;
            }
            finally
            {
                _decodingLock.Release();
            }

            return bitmap;
        }

        protected override Task<SelfDisposingBitmapDrawable> GenerateImageFromDecoderContainerAsync(IDecodedImage<Bitmap> decoded, ImageInformation imageInformation, bool isPlaceholder)
        {
            try
            {
                SelfDisposingBitmapDrawable result;

                if (decoded.IsAnimated)
                {
                    result = new FFGifDrawable(Context.Resources, decoded.AnimatedImages[0].Image, decoded.AnimatedImages);
                }
                else
                {
                    if (isPlaceholder)
                    {
                        result = new SelfDisposingBitmapDrawable(Context.Resources, decoded.Image);
                    }
                    else
                    {
                        result = new FFBitmapDrawable(Context.Resources, decoded.Image);
                    }
                }

                if (result == null || !result.HasValidBitmap)
                {
                    throw new BadImageFormatException("Not a valid bitmap");
                }

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                var javaException = ex as Java.Lang.Throwable;
                if (javaException != null && javaException.Class == Java.Lang.Class.FromType(typeof(Java.Lang.OutOfMemoryError)))
                {
                    throw new OutOfMemoryException();
                }

                throw;
            }
        }
    }
}
