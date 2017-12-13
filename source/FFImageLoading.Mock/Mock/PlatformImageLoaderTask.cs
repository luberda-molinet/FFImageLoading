using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Decoders;
using FFImageLoading.Mock;

namespace FFImageLoading.Work
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<MockBitmap, MockBitmap, TImageView> where TImageView : class
    {
        public PlatformImageLoaderTask(ITarget<MockBitmap, TImageView> target, TaskParameter parameters, IImageService imageService) : base(MockImageCache.Instance, target, parameters, imageService)
        {
        }

        protected override int DpiToPixels(int size)
        {
            return size;
        }

        protected override Task<MockBitmap> GenerateImageFromDecoderContainerAsync(IDecodedImage<MockBitmap> decoded, ImageInformation imageInformation, bool isPlaceholder)
        {
            return Task.FromResult(new MockBitmap());
        }

        protected override IDecoder<MockBitmap> ResolveDecoder(ImageInformation.ImageType type)
        {
            return new MockDecoder();
        }

        protected override Task SetTargetAsync(MockBitmap image, bool animated)
        {
            return Task.FromResult(false);
        }

        protected override Task<MockBitmap> TransformAsync(MockBitmap bitmap, IList<ITransformation> transformations, string path, ImageSource source, bool isPlaceholder)
        {
            return Task.FromResult(new MockBitmap());
        }
    }
}
