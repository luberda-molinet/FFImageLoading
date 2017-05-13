using System.Collections.Generic;
using System.Linq;
using Foundation;
using ImageIO;
using UIKit;

namespace FFImageLoading.Helpers
{
    public static class GifHelper
    {
        public static UIImage AnimateGif(NSData data)
        {
            if (data?.Length == 0)
                return null;

            using (var source = CGImageSource.FromData(data))
                return AnimateGifFromSource(source) ?? UIImage.LoadFromData(data);
        }

        private static UIImage AnimateGifFromSource(CGImageSource source)
        {
            var frameCount = source?.ImageCount;

            // no need to animate
            if (frameCount <= 1)
                return null;

            var frames = GetFrames(source);
            var delays = GetDelays(source);
            var totalDuration = delays.Sum();

            // SUPER BASIC. Does not respect variable length frames. No memory optimizations.
            return UIImage.CreateAnimatedImage(frames.ToArray(), totalDuration);
        }

        private static List<UIImage> GetFrames(CGImageSource source)
        {
            var retval = new List<UIImage>();

            for (int i = 0; i < source?.ImageCount; i++)
            {
                using (var frameImage = source.CreateImage(i, null))
                    retval.Add(UIImage.FromImage(frameImage));
            }

            return retval;
        }

        private static List<double> GetDelays(CGImageSource source)
        {
            var retval = new List<double>();

            for (int i = 0; i < source?.ImageCount; i++)
            {
                var properties = source.GetProperties(i, null);
                using (var gifProperties = properties.Dictionary["{GIF}"])
                {
                    using (var delayTime = gifProperties.ValueForKey(new NSString("DelayTime")))
                    {
                        var realDuration = double.Parse(delayTime.ToString());
                        retval.Add(realDuration);
                    }
                }
            }

            return retval;
        }

        private static int GetLoopCount(CGImageSource source)
        {
            var var = source.GetProperties(null);
            using (var gifProperties = var.Dictionary["{GIF}"])
            {
                var loopCount = gifProperties.ValueForKey(new NSString("LoopCount"));
                return int.Parse(loopCount.ToString());
            }
        }
    }
}