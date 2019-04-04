using FFImageLoading.Work;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FFImageLoading.Extensions
{
	public static class ImageExtensions
    {
        public static async Task<MemoryStream> AsRandomAccessStream(this Stream from)
        {
            var ms = new MemoryStream();
            from.Seek(0, SeekOrigin.Begin);
            await from.CopyToAsync(ms);
	        ms.Seek(0, SeekOrigin.Begin);
			return ms;
        }
        public static async Task<WriteableBitmap> ToBitmapImageAsync(this BitmapHolder holder)
        {
            if (holder?.PixelData == null)
                return null;

            WriteableBitmap writeableBitmap = null;

            await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(async () =>
            {
                writeableBitmap = await holder.ToWriteableBitmap();
            });

            return writeableBitmap;
        }

        private static Task<WriteableBitmap> ToWriteableBitmap(this BitmapHolder holder)
		{
			var wb = new WriteableBitmap(
				holder.Width,
				holder.Height,
				96,
				96,
				PixelFormats.Bgra32,
				null);
			return Task.FromResult(wb.FromByteArray(holder.PixelData));
            
        }
        public static Task<WriteableBitmap> ToWriteableBitmap(this Stream holder)
        {
			try
			{
				holder.Seek(0, SeekOrigin.Begin);
				return Task.FromResult(BitmapFactory.FromStream(holder));
			}
            catch (Exception ex)
            {
                throw;
            }
        }
		
        public async static Task<WriteableBitmap> ToBitmapImageAsync(this Stream imageStream, Tuple<int, int> downscale, bool downscaleDipUnits, InterpolationMode mode, bool allowUpscale, ImageInformation imageInformation = null)
        {
            if (imageStream == null)
                return null;

            using (var image = await imageStream.AsRandomAccessStream())
            {
                if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
                {
                    using (var downscaledImage = await image.ResizeImage(downscale.Item1, downscale.Item2, mode, downscaleDipUnits, allowUpscale, imageInformation).ConfigureAwait(false))
                    {
                        downscaledImage.Seek(0, SeekOrigin.Begin);
                        WriteableBitmap resizedBitmap = null;

                        await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(async () =>
                        {
                            resizedBitmap = await downscaledImage.ToWriteableBitmap();
                        });

                        return resizedBitmap;
                    }
                }
                else
                {
                    WriteableBitmap bitmap = null;

                    await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(async () =>
                    {
                        bitmap = await imageStream.ToWriteableBitmap();
                        if (imageInformation != null)
                        {
                            imageInformation.SetCurrentSize(bitmap.PixelWidth, bitmap.PixelHeight);
                            imageInformation.SetOriginalSize(bitmap.PixelWidth, bitmap.PixelHeight);
                        }
                    });

                    return bitmap;
                }
            }
        }

        public async static Task<BitmapHolder> ToBitmapHolderAsync(this Stream imageStream, Tuple<int, int> downscale, bool downscaleDipUnits, InterpolationMode mode, bool allowUpscale, ImageInformation imageInformation = null)
        {
            if (imageStream == null)
                return null;


            Stream src;
            using (var image = await imageStream.AsRandomAccessStream())
            {
                if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
                {
                    var downscaledImage = await image.ResizeImage(downscale.Item1, downscale.Item2, mode,
                        downscaleDipUnits, allowUpscale, imageInformation).ConfigureAwait(false);
                    {
                        src = downscaledImage;
                    }
                }
                else
                {
                    src = image;
                }
	            src.Seek(0, SeekOrigin.Begin);
	            var wb = BitmapFactory.FromStream(src);
				

                if (imageInformation != null)
                {
                    imageInformation.SetCurrentSize(wb.PixelWidth, wb.PixelHeight);
                    imageInformation.SetOriginalSize(wb.PixelWidth, wb.PixelHeight);
                }

                return new BitmapHolder(BitmapFactory.ConvertToPbgra32Format(wb).ToByteArray(), wb.PixelWidth, wb.PixelHeight);
            }
        }

        public static async Task<MemoryStream> ResizeImage(this MemoryStream imageStream, int width, int height, InterpolationMode interpolationMode, bool useDipUnits, bool allowUpscale, ImageInformation imageInformation = null)
        {
            if (useDipUnits)
            {
                width = width.DpToPixels();
                height = height.DpToPixels();
            }
            
            var wb = await imageStream.ToWriteableBitmap();
	        var widthRatio = (double)width / wb.PixelWidth;
	        var heightRatio = (double)height / wb.PixelHeight;
	        var scaleRatio = Math.Min(widthRatio, heightRatio);
	        if (width == 0)
		        scaleRatio = heightRatio;

	        if (height == 0)
		        scaleRatio = widthRatio;

			var bitmap = new TransformedBitmap(wb, new ScaleTransform(scaleRatio,scaleRatio));
            
            var bmp = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmap));
            enc.Save(bmp);

            if (imageInformation != null)
			{
				var aspectHeight = (int)Math.Floor(wb.PixelHeight * scaleRatio);
				var aspectWidth = (int)Math.Floor(wb.PixelWidth * scaleRatio);

				imageInformation.SetOriginalSize(wb.PixelWidth, wb.PixelHeight);
                imageInformation.SetCurrentSize(aspectWidth, aspectHeight);
            }
            bmp.Seek(0,SeekOrigin.Begin);
            return bmp;
        }
    }
}
