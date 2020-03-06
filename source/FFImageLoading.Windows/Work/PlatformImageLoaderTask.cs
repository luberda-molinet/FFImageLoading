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
using FFImageLoading.Decoders;
using System.Collections.Generic;

namespace FFImageLoading.Work
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<BitmapHolder, BitmapSource, TImageView> where TImageView : class
    {
        public PlatformImageLoaderTask(ITarget<BitmapSource, TImageView> target, TaskParameter parameters, IImageService imageService) : base(ImageCache.Instance, target, parameters, imageService)
        {
        }

        public async override Task Init()
        {
            await ScaleHelper.InitAsync().ConfigureAwait(false);
            await base.Init().ConfigureAwait(false);
        }

        protected override async Task SetTargetAsync(BitmapSource image, bool animated)
        {
			if (Target == null)
				return;
            
            await MainThreadDispatcher.PostAsync(() =>
            {
                ThrowIfCancellationRequested();
                PlatformTarget.Set(this, image, animated);
            }).ConfigureAwait(false);
        }

        protected override int DpiToPixels(int size)
        {
            return size.DpToPixels();
        }

        protected override IDecoder<BitmapHolder> ResolveDecoder(ImageInformation.ImageType type)
        {
            switch (type)
            {
                case ImageInformation.ImageType.GIF:
                case ImageInformation.ImageType.WEBP:
                    throw new NotImplementedException();
                default:
                    return new BaseDecoder();
            }
        }

        protected override async Task<BitmapHolder> TransformAsync(BitmapHolder bitmap, IList<ITransformation> transformations, string path, ImageSource source, bool isPlaceholder)
        {
            await StaticLocks.DecodingLock.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false); // Applying transformations is both CPU and memory intensive
            ThrowIfCancellationRequested();

            try
            {
                foreach (var transformation in transformations)
                {
                    ThrowIfCancellationRequested();

                    var old = bitmap;

                    try
                    {
                        IBitmap bitmapHolder = transformation.Transform(bitmap, path, source, isPlaceholder, Key);
                        bitmap = bitmapHolder.ToNative();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(string.Format("Transformation failed: {0}", transformation.Key), ex);
                        throw;
                    }
                    finally
                    {
                        if (old != null && old != bitmap && old.PixelData != bitmap.PixelData)
                        {
                            old.FreePixels();
                            old = null;
                        }
                    }
                }
            }
            finally
            {
				StaticLocks.DecodingLock.Release();
            }

            return bitmap;
        }

        protected override async Task<BitmapSource> GenerateImageFromDecoderContainerAsync(IDecodedImage<BitmapHolder> decoded, ImageInformation imageInformation, bool isPlaceholder)
        {
            if (decoded.IsAnimated)
            {
                throw new NotImplementedException();
            }
            else
            {
                try
                {
                    if (decoded.Image.HasWriteableBitmap)
                        return decoded.Image.WriteableBitmap;

                    return await decoded.Image.ToBitmapImageAsync().ConfigureAwait(false);
                }
                finally
                {
                    decoded.Image.FreePixels();
                    decoded.Image = null;
                }
            }
        }
    }
}
