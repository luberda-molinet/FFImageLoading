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

namespace FFImageLoading
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<SelfDisposingBitmapDrawable, TImageView> where TImageView : class
    {
        static readonly SemaphoreSlim _decodingLock = new SemaphoreSlim(1, 1);

        public PlatformImageLoaderTask(ITarget<SelfDisposingBitmapDrawable, TImageView> target, TaskParameter parameters, IImageService imageService, Configuration configuration, IMainThreadDispatcher mainThreadDispatcher)
            : base(ImageCache.Instance, configuration.DataResolverFactory ?? DataResolvers.DataResolverFactory.Instance, target, parameters, imageService, configuration, mainThreadDispatcher, true)
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
                    if (PlaceholderWeakReference != null && PlaceholderWeakReference.TryGetTarget(out placeholderDrawable) && placeholderDrawable != null)
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

        async Task<SelfDisposingBitmapDrawable> PlatformGenerateImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
        {
            Bitmap bitmap = null;

            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            ThrowIfCancellationRequested();

            // First decode with inJustDecodeBounds=true to check dimensions
            var options = new BitmapFactory.Options
            {
                InJustDecodeBounds = true
            };

            try
            {
                await BitmapFactory.DecodeStreamAsync(imageData, null, options).ConfigureAwait(false);

                ThrowIfCancellationRequested();

                options.InPurgeable = true;
                options.InJustDecodeBounds = false;
                options.InDither = true;

                imageInformation.SetOriginalSize(options.OutWidth, options.OutHeight);
                imageInformation.SetCurrentSize(options.OutWidth, options.OutHeight);

                if (!Configuration.BitmapOptimizations || (Parameters.BitmapOptimizationsEnabled.HasValue && !Parameters.BitmapOptimizationsEnabled.Value))
                {
                    // Same quality but no transparency channel. This allows to save 50% of memory: 1 pixel=2bytes instead of 4.
                    options.InPreferredConfig = Bitmap.Config.Rgb565;
                    options.InPreferQualityOverSpeed = false;
                }

                // CHECK IF BITMAP IS EXIF ROTATED
                int exifRotation = 0;
                if (source == ImageSource.Filepath)
                {
                    exifRotation = path.GetExifRotationDegrees();
                }

                ThrowIfCancellationRequested();

                // DOWNSAMPLE
                if (Parameters.DownSampleSize != null && (Parameters.DownSampleSize.Item1 > 0 || Parameters.DownSampleSize.Item2 > 0))
                {
                    // Calculate inSampleSize
                    int downsampleWidth = Parameters.DownSampleSize.Item1;
                    int downsampleHeight = Parameters.DownSampleSize.Item2;

                    // if image is rotated, swap width/height
                    if (exifRotation == 90 || exifRotation == 270)
                    {
                        downsampleWidth = Parameters.DownSampleSize.Item2;
                        downsampleHeight = Parameters.DownSampleSize.Item1;
                    }

                    if (Parameters.DownSampleUseDipUnits)
                    {
                        downsampleWidth = downsampleWidth.DpToPixels();
                        downsampleHeight = downsampleHeight.DpToPixels();
                    }

                    options.InSampleSize = CalculateInSampleSize(options, downsampleWidth, downsampleHeight, Parameters.AllowUpscale ?? Configuration.AllowUpscale);

                    if (options.InSampleSize > 1)
                        imageInformation.SetCurrentSize(
                            (int)((double)options.OutWidth / options.InSampleSize),
                            (int)((double)options.OutHeight / options.InSampleSize));

                    // If we're running on Honeycomb or newer, try to use inBitmap
                    if (Utils.HasHoneycomb())
                        AddInBitmapOptions(options);
                }

                ThrowIfCancellationRequested();

                if (!imageData.CanSeek || imageData.Position != 0)
                {
                    if (imageData.CanSeek)
                    {
                        imageData.Position = 0;
                    }
                    else
                    {
                        var resolver = DataResolverFactory.GetResolver(path, source, Parameters, Configuration);
                        var resolved = await resolver.Resolve(path, Parameters, CancellationTokenSource.Token).ConfigureAwait(false);
                        imageData?.Dispose();
                        imageData = resolved.Item1;
                    }
                }

                ThrowIfCancellationRequested();

                bitmap = await BitmapFactory.DecodeStreamAsync(imageData, null, options).ConfigureAwait(false);
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
                                old?.Recycle();
                                old?.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    _decodingLock.Release();
                }
            }

            if (isPlaceholder)
            {
                return new SelfDisposingBitmapDrawable(Context.Resources, bitmap);
            }

            return new FFBitmapDrawable(Context.Resources, bitmap);
        }

        async Task<FFGifDrawable> PlatformGenerateGifImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
        {
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            ThrowIfCancellationRequested();

            try
            {
                //TODO Add caching, transformations, downsampling, etc
                var gifDecoder = new GifDecoder();
                await gifDecoder.ReadGifAsync(imageData);
                ThrowIfCancellationRequested();
                var bitmap = gifDecoder.GetBitmap();
                ThrowIfCancellationRequested();
                return new FFGifDrawable(Context.Resources, bitmap, gifDecoder);
            }
            finally
            {
                imageData?.Dispose();
            }
        }

        protected async override Task<SelfDisposingBitmapDrawable> GenerateImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
        {
            try
            {
                SelfDisposingBitmapDrawable image = null;

                string ext = null;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    if (source == ImageSource.Url && Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute))
                        ext = System.IO.Path.GetExtension(new Uri(path).LocalPath).ToLowerInvariant();
                    else
                        ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                }

                if (source != ImageSource.Stream && ext == ".gif")
                {
                    image = await PlatformGenerateGifImageAsync(path, source, imageData, imageInformation, enableTransformations, isPlaceholder);
                }
                else
                {
                    image = await PlatformGenerateImageAsync(path, source, imageData, imageInformation, enableTransformations, isPlaceholder);
                }

                if (image == null || !image.HasValidBitmap)
                {
                    throw new BadImageFormatException("Bad image format");
                }

                return image;
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

        /// <summary>
        /// Calculate an inSampleSize for use in a {@link android.graphics.BitmapFactory.Options} object when decoding
        /// the closest inSampleSize that is a power of 2 and will result in the final decoded bitmap
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reqWidth"></param>
        /// <param name="reqHeight"></param>
        /// <param name="allowUpscale"></param>
        /// <returns></returns>
        int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight, bool allowUpscale)
        {
            // Raw height and width of image
            float height = options.OutHeight;
            float width = options.OutWidth;

            if (reqWidth == 0)
                reqWidth = (int)((reqHeight / height) * width);

            if (reqHeight == 0)
                reqHeight = (int)((reqWidth / width) * height);

            double inSampleSize = 1D;

            if (height > reqHeight || width > reqWidth || allowUpscale)
            {
                int halfHeight = (int)(height / 2);
                int halfWidth = (int)(width / 2);

                // Calculate a inSampleSize that is a power of 2 - the decoder will use a value that is a power of two anyway.
                while ((halfHeight / inSampleSize) > reqHeight && (halfWidth / inSampleSize) > reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return (int)inSampleSize;
        }

        void AddInBitmapOptions(BitmapFactory.Options options)
        {
            // inBitmap only works with mutable bitmaps so force the decoder to
            // return mutable bitmaps.
            options.InMutable = true;

            // Try and find a bitmap to use for inBitmap
            ISelfDisposingBitmapDrawable bitmapDrawable = null;
            try
            {
                bitmapDrawable = ImageCache.Instance.GetBitmapDrawableFromReusableSet(options);
                var bitmap = bitmapDrawable == null ? null : bitmapDrawable.Bitmap;

                if (bitmap != null && bitmap.Handle != IntPtr.Zero && !bitmap.IsRecycled)
                {
                    options.InBitmap = bitmapDrawable.Bitmap;
                }
            }
            finally
            {
                bitmapDrawable?.SetIsRetained(false);
            }
        }

        protected override int DpiToPixels(int size)
        {
            return size.DpToPixels();
        }
    }
}
