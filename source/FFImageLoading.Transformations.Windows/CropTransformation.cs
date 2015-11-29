using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFImageLoading.Transformations
{
    internal class CropTransformation
    {
        public static void ToCropped(BitmapHolder source, int x, int y, int width, int height)
        {
            var srcWidth = source.Width;
            var srcHeight = source.Height;

            // If the rectangle is completely out of the bitmap
            if (x > srcWidth || y > srcHeight)
            {
                return;
            }

            // Clamp to boundaries
            if (x < 0) x = 0;
            if (x + width > srcWidth) width = srcWidth - x;
            if (y < 0) y = 0;
            if (y + height > srcHeight) height = srcHeight - y;

            // Copy the pixels line by line using fast BlockCopy
            var result = new int[width * height];

            for (var line = 0; line < height; line++)
            {
                var srcOff = ((y + line) * srcWidth + x) * Helpers.SizeOfArgb;
                var dstOff = line * width * Helpers.SizeOfArgb;
                Helpers.BlockCopy(source.Pixels, srcOff, result, dstOff, width * Helpers.SizeOfArgb);
            }

            source.SetPixels(result, width, height);
        }
    }
}
