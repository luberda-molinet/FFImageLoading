using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FFImageLoading.Extensions
{
    public static class WriteableBitmapExtensions
    {
        public static async Task<Stream> AsPngStreamAsync(this WriteableBitmap bitmap)
        {
            Stream bmp = new MemoryStream();
            
            BitmapEncoder enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmap));
            enc.Save(bmp);

            return bmp;
            //using (var stream = bitmap.PixelBuffer.AsStream())
            //{
            //    pixels = new byte[(uint)stream.Length];
            //    await stream.ReadAsync(pixels, 0, pixels.Length);
            //}

            //var raStream = new InMemoryRandomAccessStream();
            //// Encode pixels into stream
            //var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, raStream);
            //encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels);
            //await encoder.FlushAsync();

            //return raStream.AsStreamForRead();
        }

        public static async Task<Stream> AsJpegStreamAsync(this WriteableBitmap bitmap, int quality = 90)
        {
            Stream bmp = new MemoryStream();

            var enc = new JpegBitmapEncoder();
            enc.QualityLevel = quality;
            enc.Frames.Add(BitmapFrame.Create(bitmap));
            enc.Save(bmp);

            return bmp;
		}
	}
}
