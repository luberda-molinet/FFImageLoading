using FFImageLoading.Helpers;
using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace FFImageLoading.Extensions
{
    public static class ImageExtensions
    {
        public static WriteableBitmap ToBitmapImage(this BitmapHolder holder)
        {
            if (holder == null || holder.Pixels == null)
                return null;

            var writeableBitmap = new WriteableBitmap(holder.Width, holder.Height);

            for (int x = 0; x < holder.Width; x++)
                for (int y = 0; y < holder.Height; y++)
                    writeableBitmap.SetPixel(x, y, holder.Pixels[x + y * holder.Width]);

            writeableBitmap.Invalidate();

            return writeableBitmap;
        }

        public async static Task<WriteableBitmap> ToBitmapImageAsync(this byte[] imageBytes, Tuple<int, int> downscale, InterpolationMode mode)
        {
            if (imageBytes == null)
                return null;

            IRandomAccessStream image = new RandomStream(imageBytes.AsBuffer().AsStream());

            if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
            {
                image = await image.ResizeImage((uint)downscale.Item1, (uint)downscale.Item2, mode).ConfigureAwait(false);
            }

            using (image)
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(image);

                image.Seek(0);

                WriteableBitmap bitmap = null;

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    bitmap.SetSource(image.AsStream());
                });

                return bitmap;
            }
        }

        public async static Task<BitmapHolder> ToBitmapHolderAsync(this byte[] imageBytes, Tuple<int, int> downscale, InterpolationMode mode)
        {
            if (imageBytes == null)
                return null;

            IRandomAccessStream image = new RandomStream(imageBytes.AsBuffer().AsStream());

            if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
            {
                image = await image.ResizeImage((uint)downscale.Item1, (uint)downscale.Item2, mode).ConfigureAwait(false);
            }

            using (image)
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(image);
                PixelDataProvider pixelDataProvider = await decoder.GetPixelDataAsync();

                var bytes = pixelDataProvider.DetachPixelData();
                int[] array = new int[decoder.PixelWidth * decoder.PixelHeight];
                CopyPixels(bytes, array);

                return new BitmapHolder(array, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
            }
        }

        private static void CopyPixels(byte[] data, int[] pixels)
        {
            int length = pixels.Length;

            for (var i = 0; i < length; i++)
            {
                pixels[i] = (data[i * 4 + 3] << 24)
                            | (data[i * 4 + 2] << 16)
                            | (data[i * 4 + 1] << 8)
                            | data[i * 4 + 0];
            }
        }

        public static async Task<IRandomAccessStream> ResizeImage(this IRandomAccessStream imageStream, uint width, uint height, InterpolationMode interpolationMode)
        {
            IRandomAccessStream resizedStream = imageStream;
            var decoder = await BitmapDecoder.CreateAsync(imageStream);
            if (decoder.OrientedPixelHeight > height || decoder.OrientedPixelWidth > width)
            {
                using (imageStream)
                {
                    resizedStream = new InMemoryRandomAccessStream();
                    BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                    double widthRatio = (double)width / decoder.OrientedPixelWidth;
                    double heightRatio = (double)height / decoder.OrientedPixelHeight;

                    double scaleRatio = Math.Min(widthRatio, heightRatio);

                    if (width == 0)
                        scaleRatio = heightRatio;

                    if (height == 0)
                        scaleRatio = widthRatio;

                    uint aspectHeight = (uint)Math.Floor(decoder.OrientedPixelHeight * scaleRatio);
                    uint aspectWidth = (uint)Math.Floor(decoder.OrientedPixelWidth * scaleRatio);

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
