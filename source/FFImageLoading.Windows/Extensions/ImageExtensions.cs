using FFImageLoading.Work;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Extensions
{
    public static class ImageExtensions
    {
        public static async Task<WriteableBitmap> ToBitmapImageAsync(this BitmapHolder holder)
        {
            if (holder == null || holder.Pixels == null)
                return null;

            WriteableBitmap writeableBitmap = null;

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                writeableBitmap = holder.ToWriteableBitmap();
                writeableBitmap.Invalidate();
            });

            return writeableBitmap;
        }

        private static unsafe WriteableBitmap ToWriteableBitmap(this BitmapHolder holder)
        {
            var writeableBitmap = new WriteableBitmap(holder.Width, holder.Height);

            using (var stream = writeableBitmap.PixelBuffer.AsStream())
            {
                int length = holder.Pixels.Length;

                var buffer = new byte[length * 4];
                fixed (int* srcPtr = holder.Pixels)
                {
                    var b = 0;
                    for (var i = 0; i < length; i++, b += 4)
                    {
                        var p = srcPtr[i];
                        buffer[b + 3] = (byte)((p >> 24) & 0xff);
                        buffer[b + 2] = (byte)((p >> 16) & 0xff);
                        buffer[b + 1] = (byte)((p >> 8) & 0xff);
                        buffer[b + 0] = (byte)(p & 0xff);
                    }
                    stream.Write(buffer, 0, length * 4);
                }

            }

            return writeableBitmap;
        }

        public async static Task<WriteableBitmap> ToBitmapImageAsync(this byte[] imageBytes, Tuple<int, int> downscale, bool downscaleDipUnits, InterpolationMode mode)
        {
            if (imageBytes == null)
                return null;

            using (var imageStream = imageBytes.AsBuffer().AsStream())
            using (IRandomAccessStream image = imageStream.AsRandomAccessStream())
            {
                if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
                {
                    using (var downscaledImage = await image.ResizeImage(downscale.Item1, downscale.Item2, mode, downscaleDipUnits).ConfigureAwait(false))
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(downscaledImage);
                        downscaledImage.Seek(0);
                        WriteableBitmap resizedBitmap = null;

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
                        {
                            resizedBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                            await resizedBitmap.SetSourceAsync(downscaledImage);
                        });

                        return resizedBitmap;
                    }
                }
                else
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(image);
                    image.Seek(0);
                    WriteableBitmap bitmap = null;

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () =>
                    {
                        bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                        await bitmap.SetSourceAsync(image);
                    });

                    return bitmap;
                }
            }
        }

        public async static Task<BitmapHolder> ToBitmapHolderAsync(this byte[] imageBytes, Tuple<int, int> downscale, bool downscaleDipUnits, InterpolationMode mode)
        {
            if (imageBytes == null)
                return null;

            using (var imageStream = imageBytes.AsBuffer().AsStream())
            using (IRandomAccessStream image = imageStream.AsRandomAccessStream())
            {
				if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
				{
                    using (var downscaledImage = await image.ResizeImage(downscale.Item1, downscale.Item2, mode, downscaleDipUnits).ConfigureAwait(false))
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(downscaledImage);
                        PixelDataProvider pixelDataProvider = await decoder.GetPixelDataAsync();

                        var bytes = pixelDataProvider.DetachPixelData();
                        int[] array = new int[decoder.PixelWidth * decoder.PixelHeight];
                        CopyPixels(bytes, array);

                        return new BitmapHolder(array, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    }
				}
                else
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(image);
                    PixelDataProvider pixelDataProvider = await decoder.GetPixelDataAsync();

                    var bytes = pixelDataProvider.DetachPixelData();
                    int[] array = new int[decoder.PixelWidth * decoder.PixelHeight];
                    CopyPixels(bytes, array);

                    return new BitmapHolder(array, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
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

        public static async Task<IRandomAccessStream> ResizeImage(this IRandomAccessStream imageStream, int width, int height, InterpolationMode interpolationMode, bool useDipUnits)
        {
            if (useDipUnits)
            {
                width = width.PointsToPixels();
                height = height.PointsToPixels();
            }

            IRandomAccessStream resizedStream = imageStream;
			var decoder = await BitmapDecoder.CreateAsync(imageStream);
            if (decoder.PixelHeight > height || decoder.PixelWidth > width)
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
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.NearestNeighbor;
                    else if (interpolationMode == InterpolationMode.Low)
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                    else if (interpolationMode == InterpolationMode.Medium)
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;
                    else if (interpolationMode == InterpolationMode.High)
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                    else
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;

                    encoder.BitmapTransform.ScaledHeight = aspectHeight;
                    encoder.BitmapTransform.ScaledWidth = aspectWidth;

					await encoder.FlushAsync();
                    resizedStream.Seek(0);
                }
            }

            return resizedStream;
        }
    }
}
