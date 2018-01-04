using System;
using System.Text;
using FFImageLoading.Work;
using System.Linq;

namespace FFImageLoading.Helpers
{
    public static class FileHeader
    {
        static readonly byte[] _jpeg = new byte[] { 255, 216, 255 };
        static readonly byte[] _png = new byte[] { 137, 80, 78, 71 };
        static readonly byte[] _svg = Encoding.UTF8.GetBytes("<");
        static readonly byte[] _webp = Encoding.UTF8.GetBytes("RIFF");
        static readonly byte[] _gif = Encoding.UTF8.GetBytes("GIF");
        static readonly byte[] _bmp = Encoding.UTF8.GetBytes("BM");
        static readonly byte[] _tiff = new byte[] { 73, 73, 42 };
        static readonly byte[] _tiff2 = new byte[] { 77, 77, 42 };
        static readonly byte[] _ico = new byte[] { 00, 00, 01, 00 };

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
