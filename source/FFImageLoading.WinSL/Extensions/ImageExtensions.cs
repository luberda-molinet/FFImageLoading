using FFImageLoading.Helpers;
using FFImageLoading.Work;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

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
                writeableBitmap = new WriteableBitmap(holder.Width, holder.Height);
                
                for (int x = 0; x < holder.Width; x++)
                    for (int y = 0; y < holder.Height; y++)
                        writeableBitmap.SetPixel(x, y, holder.Pixels[x + y * holder.Width]);

                writeableBitmap.Invalidate();
            });

            return writeableBitmap;
        }

        public async static Task<WriteableBitmap> ToBitmapImageAsync(this Stream imageStream, Tuple<int, int> downscale, bool useDipUnits, InterpolationMode mode, ImageInformation imageInformation = null)
        {
            if (imageStream == null)
                return null;

            using (IRandomAccessStream image = new RandomStream(imageStream))
            {
                if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
                {
                    using (var downscaledImage = await image.ResizeImage(downscale.Item1, downscale.Item2, mode, useDipUnits, imageInformation).ConfigureAwait(false))
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(downscaledImage);
                        downscaledImage.Seek(0);
                        WriteableBitmap resizedBitmap = null;

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                        {
                            resizedBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                            using (var s = downscaledImage.AsStream())
                            {
                                resizedBitmap.SetSource(s);
                            }
                        });

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

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                    {
                        bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                        bitmap.SetSource(imageStream);
                    });

                    return bitmap;
                }
            }
        }

        public async static Task<BitmapHolder> ToBitmapHolderAsync(this Stream imageStream, Tuple<int, int> downscale, bool useDipUnits, InterpolationMode mode, ImageInformation imageInformation = null)
        {
            if (imageStream == null)
                return null;

            using (IRandomAccessStream image = new RandomStream(imageStream))
            {
                if (downscale != null && (downscale.Item1 > 0 || downscale.Item2 > 0))
                {
                    using (var downscaledImage = await image.ResizeImage(downscale.Item1, downscale.Item2, mode, useDipUnits, imageInformation).ConfigureAwait(false))
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

                    if (imageInformation != null)
                    {
                        imageInformation.SetCurrentSize((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                        imageInformation.SetOriginalSize((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    }

                    var bytes = pixelDataProvider.DetachPixelData();
                    int[] array = new int[decoder.PixelWidth * decoder.PixelHeight];
                    CopyPixels(bytes, array);

                    return new BitmapHolder(array, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
                }
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

        public static async Task<IRandomAccessStream> ResizeImage(this IRandomAccessStream imageStream, int width, int height, InterpolationMode interpolationMode, bool useDipUnits, ImageInformation imageInformation = null)
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
