using System;
using FFImageLoading.Decoders;
using Android.Graphics;
using FFImageLoading.Work;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Drawables;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Config;
using FFImageLoading.Extensions;

namespace FFImageLoading.Decoders
{
    public class BaseDecoder : IDecoder<Bitmap>
    {
        public async Task<IDecodedImage<Bitmap>> DecodeAsync(Stream imageData, string path, ImageSource source, ImageInformation imageInformation, TaskParameter parameters)
        {
            BitmapFactory.Options options = null;
            Bitmap bitmap = null;

            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            if (options == null)
            {
                // First decode with inJustDecodeBounds=true to check dimensions
                options = new BitmapFactory.Options
                {
                    InJustDecodeBounds = true,
                };
                await BitmapFactory.DecodeStreamAsync(imageData, null, options).ConfigureAwait(false);
            }

            options.InPurgeable = true;
            options.InJustDecodeBounds = false;
            options.InDither = true;

            imageInformation.SetOriginalSize(options.OutWidth, options.OutHeight);
            imageInformation.SetCurrentSize(options.OutWidth, options.OutHeight);

            if (!Configuration.BitmapOptimizations || (parameters.BitmapOptimizationsEnabled.HasValue && !parameters.BitmapOptimizationsEnabled.Value))
            {
                // Same quality but no transparency channel. This allows to save 50% of memory: 1 pixel=2bytes instead of 4.
                options.InPreferredConfig = Bitmap.Config.Rgb565;
                options.InPreferQualityOverSpeed = false;
            }

            // CHECK IF BITMAP IS EXIF ROTATED
            int exifRotation = 0;

            if ((source == ImageSource.Filepath || source == ImageSource.Stream || source == ImageSource.Url)
                && imageInformation.Type != ImageInformation.ImageType.SVG)
            {
                try
                {
                    exifRotation = imageData.GetExifRotationDegrees();
                }
                catch (Exception ex)
                {
                    Logger.Error("Reading EXIF orientation failed", ex);
                }
            }

            if (imageData.Position != 0)
                imageData.Position = 0;

            // DOWNSAMPLE
            if (parameters.DownSampleSize != null && (parameters.DownSampleSize.Item1 > 0 || parameters.DownSampleSize.Item2 > 0))
            {
                // Calculate inSampleSize
                int downsampleWidth = parameters.DownSampleSize.Item1;
                int downsampleHeight = parameters.DownSampleSize.Item2;

                if (parameters.DownSampleUseDipUnits)
                {
                    downsampleWidth = downsampleWidth.DpToPixels();
                    downsampleHeight = downsampleHeight.DpToPixels();
                }

                options.InSampleSize = CalculateInSampleSize(options, downsampleWidth, downsampleHeight, parameters.AllowUpscale ?? Configuration.AllowUpscale);

                if (options.InSampleSize > 1)
                    imageInformation.SetCurrentSize(
                        (int)((double)options.OutWidth / options.InSampleSize),
                        (int)((double)options.OutHeight / options.InSampleSize));

                // If we're running on Honeycomb or newer, try to use inBitmap
                if (Utils.HasHoneycomb())
                    AddInBitmapOptions(options);
            }

            if (imageData.Position != 0)
                imageData.Position = 0;

            bitmap = await BitmapFactory.DecodeStreamAsync(imageData, null, options).ConfigureAwait(false);

            // if image is rotated, swap width/height
            if (exifRotation != 0)
            {
                var oldBitmap = bitmap;
                bitmap = bitmap.ToRotatedBitmap(exifRotation);
                ImageCache.Instance.AddToReusableSet(new SelfDisposingBitmapDrawable(oldBitmap) { InCacheKey = Guid.NewGuid().ToString() });
            }

            return new DecodedImage<Bitmap>() { Image = bitmap };
        }

        public Configuration Configuration => ImageService.Instance.Config;

        public IMiniLogger Logger => ImageService.Instance.Config.Logger;

        /// <summary>
        /// Calculate an inSampleSize for use in a {@link android.graphics.BitmapFactory.Options} object when decoding
        /// the closest inSampleSize that is a power of 2 and will result in the final decoded bitmap
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reqWidth"></param>
        /// <param name="reqHeight"></param>
        /// <param name="allowUpscale"></param>
        /// <returns></returns>
        public static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight, bool allowUpscale)
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
    }
}
