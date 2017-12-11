using System;
using System.IO;
using AppKit;

namespace FFImageLoading.Helpers
{
    public class WebPDecoder : IImageFileDecoder<NSImage>
    {
        WebP.Mac.WebPCodec _decoder = new WebP.Mac.WebPCodec();

        public NSImage Decode(Stream stream)
        {
            return _decoder.Decode(stream);
        }
    }
}
