using FFImageLoading.Helpers;
using FFImageLoading.Work;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Extensions
{
    public static class ImageExtensions
    {
        public static async Task<WriteableBitmap> ToBitmapImageAsync(this BitmapHolder holder)
        {
            if (holder == null || holder.PixelData == null)
                return null;

            WriteableBitmap writeableBitmap = null;

            await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(async () =>
            {
                writeableBitmap = await holder.ToWriteableBitmap();
                writeableBitmap.Invalidate();
            }).ConfigureAwait(false);

            return writeableBitmap;
        }

        private static async Task<WriteableBitmap> ToWriteableBitmap(this BitmapHolder holder)
        {
            var writeableBitmap = new WriteableBitmap(holder.Width, holder.Height);

            using (var stream = writeableBitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(holder.PixelData, 0, holder.PixelData.Length);
            }

            return writeableBitmap;
        }

        public async static Task<WriteableBitmap> ToBitmapImageAsync(this Stream imageStream, Tuple<int, int> downscale, bool downscaleDipUnits, InterpolationMode mode, bool allowUpscale, ImageInformation imageInformation = null)
        {
            if (imageStream == null)
                return null;

            using (IRandomAccessStream image = imageStream.AsRandomAccessStream())
            {
                if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
                {
                    using (var downscaledImage = await image.ResizeImage(downscale.Item1, downscale.Item2, mode, downscaleDipUnits, allowUpscale, imageInformation).ConfigureAwait(false))
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(downscaledImage);
                        downscaledImage.Seek(0);
                        WriteableBitmap resizedBitmap = null;

                        await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(async () =>
                        {
                            resizedBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                            await resizedBitmap.SetSourceAsync(downscaledImage);
                        }).ConfigureAwait(false);

                        return resizedBitmap;
                    }
                }
                else
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(image);
                    image.Seek(0);
                    WriteableBitmap bitmap = null;

                    if (imageInformation != null)
                    {
                        imageInformation.SetCurrentSize((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                        imageInformation.SetOriginalSize((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    }

                    await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(async () =>
                    {
                        bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                        await bitmap.SetSourceAsync(image);
                    }).ConfigureAwait(false);

                    return bitmap;
                }
            }
        }

        public async static Task<BitmapHolder> ToBitmapHolderAsync(this Stream imageStream, Tuple<int, int> downscale, bool downscaleDipUnits, InterpolationMode mode, bool allowUpscale, ImageInformation imageInformation = null)
        {
            if (imageStream == null)
                return null;

            using (IRandomAccessStream image = imageStream.AsRandomAccessStream())
            {
                if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
                {
                    using (var downscaledImage = await image.ResizeImage(downscale.Item1, downscale.Item2, mode, downscaleDipUnits, allowUpscale, imageInformation).ConfigureAwait(false))
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(downscaledImage);
                        PixelDataProvider pixelDataProvider = await decoder.GetPixelDataAsync(
                            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, new BitmapTransform(),
                            ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);

                        var bytes = pixelDataProvider.DetachPixelData();

                        return new BitmapHolder(bytes, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    }
                }
                else
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(image);
                    PixelDataProvider pixelDataProvider = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, new BitmapTransform(), 
                        ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);

                    if (imageInformation != null)
                    {
                        imageInformation.SetCurrentSize((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                        imageInformation.SetOriginalSize((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    }

                    var bytes = pixelDataProvider.DetachPixelData();

                    return new BitmapHolder(bytes, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
                }
            }
        }

        private static unsafe void CopyPixels(byte[] data, int[] pixels)
        {
            int length = pixels.Length;

            fixed (byte* srcPtr = data)
            {
                fixed (int* dstPtr = pixels)
                {
                    for (var i = 0; i < length; i++)
                    {
                        dstPtr[i] = (srcPtr[i * 4 + 3] << 24)
                                  | (srcPtr[i * 4 + 2] << 16)
                                  | (srcPtr[i * 4 + 1] << 8)
                                  | srcPtr[i * 4 + 0];
                    }
                }
            }
        }

        public static async Task<IRandomAccessStream> ResizeImage(this IRandomAccessStream imageStream, int width, int height, InterpolationMode interpolationMode, bool useDipUnits, bool allowUpscale, ImageInformation imageInformation = null)
        {
            if (useDipUnits)
            {
                width = width.DpToPixels();
                height = height.DpToPixels();
            }

            IRandomAccessStream resizedStream = imageStream;
            var decoder = await BitmapDecoder.CreateAsync(imageStream);
            if ((height > 0 && decoder.PixelHeight > height) || (width > 0 && decoder.PixelWidth > width) || allowUpscale)
            {
                using (imageStream)
                {
                    resizedStream = new InMemoryRandomAccessStream();
                    BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                    
                    double widthRatio = (double)width / decoder.PixelWidth;
                    double heightRatio = (double)height / decoder.PixelHeight;

                    double scaleRatio = Math.Min(widthRatio, heightRatio);

                    if (width == 0)
                        scaleRatio = heightRatio;

                    if (height == 0)
                        scaleRatio = widthRatio;

                    uint aspectHeight = (uint)Math.Floor(decoder.PixelHeight * scaleRatio);
                    uint aspectWidth = (uint)Math.Floor(decoder.PixelWidth * scaleRatio);

                    if (interpolationMode == InterpolationMode.None)
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;
                    else if (interpolationMode == InterpolationMode.Low)
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                    else if (interpolationMode == InterpolationMode.Medium)
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;
                    else if (interpolationMode == InterpolationMode.High)
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                    else
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;

                    encoder.BitmapTransform.ScaledHeight = aspectHeight;
                    encoder.BitmapTransform.ScaledWidth = aspectWidth;

                    if (imageInformation != null)
                    {
                        imageInformation.SetOriginalSize((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                        imageInformation.SetCurrentSize((int)aspectWidth, (int)aspectHeight);
                    }

                    await encoder.FlushAsync();
                    resizedStream.Seek(0);
                }
            }

            return resizedStream;
        }
    }
}
