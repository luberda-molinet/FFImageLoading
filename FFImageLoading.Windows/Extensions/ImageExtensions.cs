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
        public unsafe async static Task<WriteableBitmap> ToBitmapImageAsync(this BitmapHolder holder)
        {
            if (holder == null || holder.Pixels == null)
                return null;

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
            writeableBitmap.Invalidate();

            return writeableBitmap;
        }

        public async static Task<WriteableBitmap> ToBitmapImageAsync(this byte[] imageBytes)
        {
            if (imageBytes == null)
                return null;

            using (var image = imageBytes.AsBuffer().AsStream().AsRandomAccessStream())
            {
                var decoder = await BitmapDecoder.CreateAsync(image);
                image.Seek(0);

                WriteableBitmap bitmap = null;

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    bitmap.SetSource(image);
                });

                return bitmap;
            }
        }

        public async static Task<BitmapHolder> ToBitmapHolderAsync(this byte[] imageBytes)
        {
            if (imageBytes == null)
                return null;

            using (var image = imageBytes.AsBuffer().AsStream().AsRandomAccessStream())
            {
                var decoder = await BitmapDecoder.CreateAsync(image);

                image.Seek(0);
                int[] array = null;

                WriteableBitmap bitmap = null;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                    bitmap.SetSource(image);

                    var bytes = bitmap.PixelBuffer.ToArray();

                    array = new int[bitmap.PixelWidth * bitmap.PixelHeight];
                    CopyPixels(bytes, array);
                });

                return new BitmapHolder(array, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
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

        //public static unsafe void BlockCopy(Array src, int srcOffset, WriteableBitmap dest, int destOffset, int count)
        //{
        //    int length = dest.PixelWidth * dest.PixelHeight;
        //    var pixels = new int[length];

        //    var data = dest.PixelBuffer.ToArray();
        //    fixed (byte* srcPtr = data)
        //    {
        //        fixed (int* dstPtr = pixels)
        //        {
        //            for (var i = 0; i < length; i++)
        //            {
        //                dstPtr[i] = (srcPtr[i * 4 + 3] << 24)
        //                          | (srcPtr[i * 4 + 2] << 16)
        //                          | (srcPtr[i * 4 + 1] << 8)
        //                          | srcPtr[i * 4 + 0];
        //            }
        //        }
        //    }

        //    System.Buffer.BlockCopy(src, srcOffset, pixels, destOffset, count);
        //}

        ///// <summary>
        ///// Method compressing image stored in stream
        ///// </summary>
        ///// <param name="sourceStream">stream with the image</param>
        ///// <param name="quality">new quality of the image 0.0 - 1.0</param>
        ///// <returns></returns>
        //public static async Task<IRandomAccessStream> CompressImageAsync(IRandomAccessStream sourceStream, double newQuality)
        //{
        //    // create bitmap decoder from source stream
        //    BitmapDecoder bmpDecoder = await BitmapDecoder.CreateAsync(sourceStream);

        //    // bitmap transform if you need any
        //    BitmapTransform bmpTransform = new BitmapTransform() { ScaledHeight = newHeight, ScaledWidth = newWidth, InterpolationMode = BitmapInterpolationMode.Cubic };

        //    PixelDataProvider pixelData = await bmpDecoder.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, bmpTransform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
        //    InMemoryRandomAccessStream destStream = new InMemoryRandomAccessStream(); // destination stream

        //    // define new quality for the image
        //    var propertySet = new BitmapPropertySet();
        //    var quality = new BitmapTypedValue(newQuality, PropertyType.Single);
        //    propertySet.Add("ImageQuality", quality);

        //    // create encoder with desired quality
        //    BitmapEncoder bmpEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, destFileStream, propertySet);
        //    bmpEncoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, newHeight, newWidth, 300, 300, pixelData.DetachPixelData());
        //    await bmpEncoder.FlushAsync();
        //    return destStream;
        //}
    }
}
