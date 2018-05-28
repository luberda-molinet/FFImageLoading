using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Extensions
{
    public static class WriteableBitmapExtensions
    {
        public static async Task<Stream> AsPngStreamAsync(this WriteableBitmap bitmap)
        {
            byte[] pixels;
            using (var stream = bitmap.PixelBuffer.AsStream())
            {
                pixels = new byte[(uint)stream.Length];
                await stream.ReadAsync(pixels, 0, pixels.Length);
            }

            var raStream = new InMemoryRandomAccessStream();
            // Encode pixels into stream
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, raStream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync();

            return raStream.AsStreamForRead();
        }

        public static async Task<Stream> AsJpegStreamAsync(this WriteableBitmap bitmap, int quality = 90)
        {
            byte[] pixels;
            using (var stream = bitmap.PixelBuffer.AsStream())
            {
                pixels = new byte[(uint)stream.Length];
                await stream.ReadAsync(pixels, 0, pixels.Length);
            }

            var propertySet = new BitmapPropertySet();
            var qualityValue = new BitmapTypedValue((double)quality / 100d, Windows.Foundation.PropertyType.Single);
            propertySet.Add("ImageQuality", qualityValue);

            var raStream = new InMemoryRandomAccessStream();
            // Encode pixels into stream
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, raStream, propertySet);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync();

            return raStream.AsStreamForRead();
        }
    }
}
