using System;
using System.IO;
using UIKit;

namespace FFImageLoading.Helpers
{
    public class WebPDecoder : IImageFileDecoder<UIImage>
    {
        WebP.Touch.WebPCodec _decoder = new WebP.Touch.WebPCodec();

        public UIImage Decode(Stream stream)
        {
            return _decoder.Decode(stream);
        }
    }
}
