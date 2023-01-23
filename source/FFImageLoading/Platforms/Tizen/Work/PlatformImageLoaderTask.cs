using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using FFImageLoading.Cache;
using FFImageLoading.Views;
using ElmSharp;
using FFImageLoading.Decoders;
using System.Collections.Generic;
using System.Threading;
using FFImageLoading.Extensions;
using FFImageLoading.Helpers;

namespace FFImageLoading.Work
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<SharedEvasImage, SharedEvasImage, TImageView> where TImageView : class
    {
        public PlatformImageLoaderTask(ITarget<SharedEvasImage, TImageView> target, TaskParameter parameters, IImageService imageService) : base(EvasImageCache.Instance, target, parameters, imageService)
        {
        }

        protected override int DpiToPixels(int size)
        {
            return size.DpToPixels();
        }

        protected override async Task SetTargetAsync(SharedEvasImage image, bool animated)
        {
			if (Target == null)
				return;

            await MainThreadDispatcher.PostAsync(() =>
            {
                ThrowIfCancellationRequested();
                PlatformTarget.Set(this, image, animated);
            }).ConfigureAwait(false);
        }

        protected override IDecoder<SharedEvasImage> ResolveDecoder(ImageInformation.ImageType type)
        {
            switch (type)
            {
                default:
                    return new BaseDecoder();
            }
        }

        protected override async Task<SharedEvasImage> TransformAsync(SharedEvasImage bitmap, IList<ITransformation> transformations, string path, ImageSource source, bool isPlaceholder)
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
                            //TODO Is it neccessary?
                            //old.DisposeOnMainThread();
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

        protected override Task<SharedEvasImage> GenerateImageFromDecoderContainerAsync(IDecodedImage<SharedEvasImage> decoded, ImageInformation imageInformation, bool isPlaceholder)
        {
            if (decoded.IsAnimated)
            {
                throw new NotImplementedException();
            }
            else
            {
                return Task.FromResult(decoded.Image);
            }
        }
    }
}
