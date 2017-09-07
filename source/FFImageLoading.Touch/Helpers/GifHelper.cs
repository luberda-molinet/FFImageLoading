using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ImageIO;
using UIKit;
using FFImageLoading.Work;

namespace FFImageLoading.Helpers
{
    // Converted from: https://github.com/mayoff/uiimage-from-animated-gif/blob/master/uiimage-from-animated-gif/UIImage%2BanimatedGIF.m
    public static class GifHelper
    {
        public static UIImage AnimateGif(CGImageSource source, nfloat scale, CGImageThumbnailOptions options, TaskParameter parameters)
        {
            if (source == null)
                return null;

            var frameCount = source.ImageCount;

            // no need to animate, fail safe.
            if (frameCount <= 1)
            {
                using (var imageRef = source.CreateThumbnail(0, options))
                {
                    return new UIImage(imageRef, scale, UIImageOrientation.Up);
                }
            }

            var frames = GetFrames(source, options);
            var delays = GetDelays(source);
            var totalDuration = delays.Sum();
            var adjustedFrames = AdjustFramesToSpoofDurations(frames, scale, delays, totalDuration);

            return UIImage.CreateAnimatedImage(adjustedFrames.ToArray(), totalDuration / 100.0);
        }

        private static List<CoreGraphics.CGImage> GetFrames(CGImageSource source, CGImageThumbnailOptions options)
        {
            var retval = new List<CoreGraphics.CGImage>();

            for (int i = 0; i < source?.ImageCount; i++)
            {
                var frameImage = source.CreateThumbnail(i, options);
                retval.Add(frameImage);
            }

            return retval;
        }

        private static List<int> GetDelays(CGImageSource source)
        {
            var retval = new List<int>();

            for (int i = 0; i < source?.ImageCount; i++)
            {
                var delayCentiseconds = 1;
                var properties = source.GetProperties(i, null);
                using (var gifProperties = properties.Dictionary[CGImageProperties.GIFDictionary])
                {
                    if (gifProperties != null)
                    {
                        using (var unclampedDelay = gifProperties.ValueForKey(CGImageProperties.GIFUnclampedDelayTime))
                        {
                            double delayAsDouble = unclampedDelay != null ? double.Parse(unclampedDelay.ToString(), CultureInfo.InvariantCulture) : 0;

                            if (delayAsDouble == 0)
                            {
                                using (var delay = gifProperties.ValueForKey(CGImageProperties.GIFDelayTime))
                                    delayAsDouble = delay != null ? double.Parse(delay.ToString(), CultureInfo.InvariantCulture) : 0;
                            }

                            if (delayAsDouble > 0)
                                delayCentiseconds = (int)(delayAsDouble * 100);
                        }
                    }
                }

                retval.Add(delayCentiseconds);
            }

            return retval;
        }

        /* The GIF stores a separate duration for each frame, in units of centiseconds (hundredths of a second).  However, a `UIImage` only has a single, total `duration` property, which is a floating-point number.
         * To handle this mismatch, I add each source image (from the GIF) to `animation` a varying number of times to match the ratios between the frame durations in the GIF.
         * For example, suppose the GIF contains three frames.  Frame 0 has duration 3.  Frame 1 has duration 9.  Frame 2 has duration 15.  I divide each duration by the greatest common denominator of all the durations,
         * which is 3, and add each frame the resulting number of times.  Thus `animation` will contain frame 0 3/3 = 1 time, then frame 1 9/3 = 3 times, then frame 2 15/3 = 5 times.
         * I set `animation.duration` to (3+9+15)/100 = 0.27 seconds. */
        private static List<UIImage> AdjustFramesToSpoofDurations(List<CoreGraphics.CGImage> images, nfloat scale, List<int> delays, int totalDuration)
        {
            var count = images.Count;
            var gcd = GetGCD(delays);
            var frameCount = totalDuration / gcd;
            var frames = new UIImage[frameCount];
            var f = 0;

            for (var i = 0; i < count; i++)
            {
                var frame = UIImage.FromImage(images[i], scale, UIImageOrientation.Up);
                for (var j = delays[i] / gcd; j > 0; --j)
                    frames[f++] = frame;
            }

            return frames.ToList();
        }

        private static int GetGCD(List<int> delays)
        {
            var gcd = delays[0];

            for (var i = 1; i < delays.Count; ++i)
                gcd = PairGCD(delays[i], gcd);

            return gcd;
        }

        private static int PairGCD(int a, int b)
        {
            if (a < b)
                return PairGCD(b, a);

            while (true)
            {
                var r = a % b;
                if (r == 0)
                    return b;

                a = b;
                b = r;
            }
        }
    }
}
