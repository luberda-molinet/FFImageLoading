using System;
using UIKit;

namespace FFImageLoading.Extensions
{
    public static class UIImageExtensions
    {
        public static nuint GetMemorySize(this UIImage image)
        {
            return (nuint)(image.CGImage.BytesPerRow * image.CGImage.Height);
        }
    }
}

