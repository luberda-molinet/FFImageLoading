using System;
using System.Text;
using FFImageLoading.Work;
using System.Linq;

namespace FFImageLoading.Helpers
{
    public static class FileHeader
    {
        private static readonly byte[] _jpeg = { 255, 216, 255 };
        private static readonly byte[] _png = { 137, 80, 78, 71 };
        private static readonly byte[] _svg = Encoding.UTF8.GetBytes("<");
        private static readonly byte[] _webp = Encoding.UTF8.GetBytes("RIFF");
        private static readonly byte[] _gif = Encoding.UTF8.GetBytes("GIF");
        private static readonly byte[] _bmp = Encoding.UTF8.GetBytes("BM");
        private static readonly byte[] _tiff = { 73, 73, 42 };
        private static readonly byte[] _tiff2 = { 77, 77, 42 };
        private static readonly byte[] _ico = { 00, 00, 01, 00 };

        public static ImageInformation.ImageType GetImageType(byte[] header)
        {
            if (_jpeg.SequenceEqual(header.Take(_jpeg.Length)))
                return ImageInformation.ImageType.JPEG;

            if (_png.SequenceEqual(header.Take(_png.Length)))
                return ImageInformation.ImageType.PNG;

            if (_svg.SequenceEqual(header.Take(_svg.Length)))
                return ImageInformation.ImageType.SVG;

            if (_webp.SequenceEqual(header.Take(_webp.Length)))
                return ImageInformation.ImageType.WEBP;

            if (_gif.SequenceEqual(header.Take(_gif.Length)))
                return ImageInformation.ImageType.GIF;

            if (_bmp.SequenceEqual(header.Take(_bmp.Length)))
                return ImageInformation.ImageType.BMP;

            if (_tiff.SequenceEqual(header.Take(_tiff.Length)))
                return ImageInformation.ImageType.TIFF;

            if (_tiff2.SequenceEqual(header.Take(_tiff2.Length)))
                return ImageInformation.ImageType.TIFF;

            if (_ico.SequenceEqual(header.Take(_ico.Length)))
                return ImageInformation.ImageType.ICO;

            return ImageInformation.ImageType.Unknown;
        }
    }
}
