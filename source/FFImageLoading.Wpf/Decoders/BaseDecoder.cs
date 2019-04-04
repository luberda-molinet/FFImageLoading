using FFImageLoading.Config;
using FFImageLoading.Extensions;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FFImageLoading.Decoders
{
    public class BaseDecoder : IDecoder<BitmapHolder>
    {
        public async Task<IDecodedImage<BitmapHolder>> DecodeAsync(Stream imageData, string path, ImageSource source, ImageInformation imageInformation, TaskParameter parameters)
        {
            BitmapHolder imageIn = null;

            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            bool allowUpscale = parameters.AllowUpscale ?? Configuration.AllowUpscale;

            if (parameters.Transformations == null || parameters.Transformations.Count == 0)
            {
                var bitmap = await imageData.ToBitmapImageAsync(parameters.DownSampleSize, parameters.DownSampleUseDipUnits, parameters.DownSampleInterpolationMode, allowUpscale, imageInformation).ConfigureAwait(false);
                imageIn = new BitmapHolder(bitmap);
            }
            else
            {
                imageIn = await imageData.ToBitmapHolderAsync(parameters.DownSampleSize, parameters.DownSampleUseDipUnits, parameters.DownSampleInterpolationMode, allowUpscale, imageInformation).ConfigureAwait(false);
            }

            return new DecodedImage<BitmapHolder>() { Image = imageIn };
        }

        public Configuration Configuration => ImageService.Instance.Config;

        public IMiniLogger Logger => ImageService.Instance.Config.Logger;
    }
}
