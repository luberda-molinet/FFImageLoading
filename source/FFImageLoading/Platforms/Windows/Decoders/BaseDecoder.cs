using FFImageLoading.Config;
using FFImageLoading.Extensions;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FFImageLoading.Decoders
{
    public class BaseDecoder : IDecoder<BitmapHolder>
    {
		public BaseDecoder(IImageService<BitmapSource> imageService)
		{
			this.imageService = imageService;
		}

		protected IImageService<BitmapSource> imageService;

        public async Task<IDecodedImage<BitmapHolder>> DecodeAsync(Stream imageData, string path, Work.ImageSource source, ImageInformation imageInformation, TaskParameter parameters)
        {
            BitmapHolder imageIn = null;

            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            bool allowUpscale = parameters.AllowUpscale ?? imageService.Configuration.AllowUpscale;

            if (parameters.Transformations == null || parameters.Transformations.Count == 0)
            {
                var bitmap = await imageData.ToBitmapImageAsync(imageService, parameters.Scale, parameters.DownSampleSize, parameters.DownSampleUseDipUnits, parameters.DownSampleInterpolationMode, allowUpscale, imageInformation).ConfigureAwait(false);
                imageIn = new BitmapHolder(bitmap);
            }
            else
            {
                imageIn = await imageData.ToBitmapHolderAsync(imageService, parameters.Scale, parameters.DownSampleSize, parameters.DownSampleUseDipUnits, parameters.DownSampleInterpolationMode, allowUpscale, imageInformation).ConfigureAwait(false);
            }

            return new DecodedImage<BitmapHolder>() { Image = imageIn };
        }
    }
}
