using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ImageIO;
using FFImageLoading.Work;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using FFImageLoading.Config;
using FFImageLoading.Helpers;
using Foundation;
using CoreGraphics;
using FFImageLoading.Helpers.Gif;
using System.Threading;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif

namespace FFImageLoading.Decoders
{
    public class GifDecoder : IDecoder<PImage>
    {
		private static readonly SemaphoreSlim _gifLock = new SemaphoreSlim(1, 1);

        public async Task<IDecodedImage<PImage>> DecodeAsync(Stream stream, string path, ImageSource source, ImageInformation imageInformation, TaskParameter parameters)
        {
            var downsampleWidth = parameters.DownSampleSize?.Item1 ?? 0;
            var downsampleHeight = parameters.DownSampleSize?.Item2 ?? 0;
            var allowUpscale = parameters.AllowUpscale ?? Configuration.AllowUpscale;

            if (parameters.DownSampleUseDipUnits)
            {
                downsampleWidth = downsampleWidth.DpToPixels();
                downsampleHeight = downsampleHeight.DpToPixels();
            }

            using (var nsdata = NSData.FromStream(stream))
            {
                var result = await SourceRegfToDecodedImageAsync(nsdata, new CGSize(downsampleWidth, downsampleHeight), ScaleHelper.Scale,
                                                      Configuration, parameters, RCTResizeMode.ScaleAspectFill, imageInformation, allowUpscale).ConfigureAwait(false);
				return result;
            }
        }

        public static async Task<IDecodedImage<PImage>> SourceRegfToDecodedImageAsync(NSData nsdata, CGSize destSize, nfloat destScale, Configuration config, TaskParameter parameters, RCTResizeMode resizeMode = RCTResizeMode.ScaleAspectFit, ImageInformation imageinformation = null, bool allowUpscale = false)
        {
            using (var sourceRef = CGImageSource.FromData(nsdata))
            {

                if (sourceRef == null)
                    throw new BadImageFormatException("Decoded image is null or corrupted");

                var imageProperties = sourceRef.GetProperties(0);

                if (imageProperties == null || !imageProperties.PixelWidth.HasValue || !imageProperties.PixelHeight.HasValue)
                    throw new BadImageFormatException("Can't read image size properties. File corrupted?");

                imageinformation.SetOriginalSize(imageProperties.PixelWidth.Value, imageProperties.PixelHeight.Value);

                var sourceSize = new CGSize(imageProperties.PixelWidth.Value, imageProperties.PixelHeight.Value);

                if (destSize.IsEmpty)
                {
                    destSize = sourceSize;
                    if (destScale <= 0)
                    {
                        destScale = 1;
                    }
                }
                else if (destScale <= 0)
                {
                    destScale = ScaleHelper.Scale;
                }

                // Calculate target size
                var targetSize = RCTTargetSize(sourceSize, 1, destSize, destScale, resizeMode, allowUpscale);
                var targetPixelSize = RCTSizeInPixels(targetSize, destScale);
                var maxPixelSize = (int)Math.Max(
                    allowUpscale ? targetPixelSize.Width : Math.Min(sourceSize.Width, targetPixelSize.Width),
                    allowUpscale ? targetPixelSize.Height : Math.Min(sourceSize.Height, targetPixelSize.Height)
                );

                var options = new CGImageThumbnailOptions()
                {
                    ShouldAllowFloat = true,
                    CreateThumbnailWithTransform = true,
                    CreateThumbnailFromImageAlways = true,
                    MaxPixelSize = maxPixelSize,
                    ShouldCache = false,
                };

                PImage image = null;
                IAnimatedImage<PImage>[] images = null;

                // GIF
                if (sourceRef.ImageCount > 1 && config.AnimateGifs && imageinformation.Type != ImageInformation.ImageType.ICO)
                {
					try
					{
						await _gifLock.WaitAsync().ConfigureAwait(false);

						var frameCount = sourceRef.ImageCount;

						// no need to animate, fail safe.
						if (frameCount <= 1)
						{
							using (var imageRef = sourceRef.CreateThumbnail(0, options))
							{
#if __IOS__
								image = new PImage(imageRef, destScale, UIImageOrientation.Up);
#elif __MACOS__
                                image = new PImage(imageRef, CGSize.Empty);
#endif
							}
						}

#if __IOS__
						var frames = GetFrames(sourceRef, options);
						var delays = GetDelays(sourceRef);
						var totalDuration = delays.Sum();
						var adjustedFrames = AdjustFramesToSpoofDurations(frames, destScale, delays, totalDuration);
						var avgDuration = (double)totalDuration / adjustedFrames.Length;

						images = new AnimatedImage<PImage>[adjustedFrames.Length];

						for (int i = 0; i < images.Length; i++)
						{
							images[i] = new AnimatedImage<PImage>() { Image = adjustedFrames[i], Delay = (int)avgDuration };
						}
#elif __MACOS__
                        images = new AnimatedImage<PImage>[frameCount];
						var delays = GetDelays(sourceRef);

						for (var i = 0; i < images.Length; i++)
                        {
                            var nsImage = new PImage(sourceRef.CreateThumbnail(i, options), CGSize.Empty);
                            images[i] = new AnimatedImage<PImage>() { Image = nsImage, Delay = delays[i] };
                        }
#endif
					}
					finally
					{
						_gifLock.Release();
					}
                }
                else
                {

                    // Get thumbnail
                    using (var imageRef = sourceRef.CreateThumbnail(0, options))
                    {
                        if (imageRef != null)
                        {
#if __IOS__
                        image = new PImage(imageRef, destScale, UIImageOrientation.Up);
#elif __MACOS__
                            image = new PImage(imageRef, CGSize.Empty);
#endif
                        }
                    }
                }

                var result = new DecodedImage<PImage>();

                if (images != null && images.Length > 1)
                {
                    result.IsAnimated = true;
                    result.AnimatedImages = images;

                    if (imageinformation != null)
                    {
                        var width = (int)images[0].Image.Size.Width;
                        var height = (int)images[0].Image.Size.Height;
                        imageinformation.SetCurrentSize(width.DpToPixels(), height.DpToPixels());
                    }
                }
                else
                {
                    result.Image = image;

                    if (imageinformation != null)
                    {
                        var width = (int)image.Size.Width;
                        var height = (int)image.Size.Height;
                        imageinformation.SetCurrentSize(width.DpToPixels(), height.DpToPixels());
                    }
                }

                return result;
            }
        }

        public Configuration Configuration => ImageService.Instance.Config;

        public IMiniLogger Logger => ImageService.Instance.Config.Logger;

        public enum RCTResizeMode : long
        {
#if __MACOS__
            ScaleAspectFill = (long)NSImageScaling.ProportionallyUpOrDown, //UIViewContentMode.ScaleAspectFill,
            ScaleAspectFit = (long)NSImageScaling.ProportionallyDown, //UIViewContentMode.ScaleAspectFit,
            ScaleToFill = (long)NSImageScaling.AxesIndependently //UIViewContentMode.ScaleToFill,
#elif __IOS__
            ScaleAspectFill = UIViewContentMode.ScaleAspectFill,
            ScaleAspectFit = UIViewContentMode.ScaleAspectFit,
            ScaleToFill = UIViewContentMode.ScaleToFill,
#endif
        }

        private static List<int> GetDelays(CGImageSource source)
        {
            var retval = new List<int>();

            for (int i = 0; i < source?.ImageCount; i++)
            {
                var delayMs = 0;
                var properties = source.GetProperties(i, null);
                using (var gifProperties = properties.Dictionary[ImageIO.CGImageProperties.GIFDictionary])
                {
                    if (gifProperties != null)
                    {
                        using (var unclampedDelay = gifProperties.ValueForKey(ImageIO.CGImageProperties.GIFUnclampedDelayTime))
                        {
                            var delayAsDouble = unclampedDelay != null ? double.Parse(unclampedDelay.ToString(), CultureInfo.InvariantCulture) : 0;

                            if (Math.Abs(delayAsDouble) < double.Epsilon)
                            {
                                using (var delay = gifProperties.ValueForKey(ImageIO.CGImageProperties.GIFDelayTime))
                                    delayAsDouble = delay != null ? double.Parse(delay.ToString(), CultureInfo.InvariantCulture) : 0;
                            }

                            if (delayAsDouble > 0)
								delayMs = (int)(delayAsDouble * 1000);
                        }
                    }
                }

                retval.Add(GifHelper.GetValidFrameDelay(delayMs));
            }

            return retval;
        }

        private static CGSize RCTTargetSize(CGSize sourceSize, nfloat sourceScale, CGSize destSize, nfloat destScale, RCTResizeMode resizeMode, bool allowUpscaling)
        {
            switch (resizeMode)
            {
                case RCTResizeMode.ScaleToFill:

                    if (!allowUpscaling)
                    {
                        var scale = sourceScale / destScale;
                        destSize.Width = (nfloat)Math.Min(sourceSize.Width * scale, destSize.Width);
                        destSize.Height = (nfloat)Math.Min(sourceSize.Height * scale, destSize.Height);
                    }
                    return RCTCeilSize(destSize, destScale);

                default:
                    {
                        var size = RCTTargetRect(sourceSize, destSize, destScale, resizeMode).Size;
                        if (!allowUpscaling)
                        {
                            // return sourceSize if target size is larger
                            if (sourceSize.Width * sourceScale < size.Width * destScale)
                            {
                                return sourceSize;
                            }
                        }
                        return size;
                    }
            }
        }

        private static CGSize RCTSizeInPixels(CGSize pointSize, nfloat scale) => new CGSize(Math.Ceiling(pointSize.Width * scale), Math.Ceiling(pointSize.Height * scale));
        private static CGSize RCTCeilSize(CGSize size, nfloat scale) => new CGSize(RCTCeilValue(size.Width, scale), RCTCeilValue(size.Height, scale));
        private static nfloat RCTCeilValue(nfloat value, nfloat scale) => (nfloat)Math.Ceiling(value * scale) / scale;
        private static nfloat RCTFloorValue(nfloat value, nfloat scale) => (nfloat)Math.Floor(value * scale) / scale;

        private static CGRect RCTTargetRect(CGSize sourceSize, CGSize destSize, nfloat destScale, RCTResizeMode resizeMode)
        {
            if (destSize.IsEmpty)
            {
                // Assume we require the largest size available
                return new CGRect(CGPoint.Empty, sourceSize);
            }

            var aspect = sourceSize.Width / sourceSize.Height;
            // If only one dimension in destSize is non-zero (for example, an Image
            // with `flex: 1` whose height is indeterminate), calculate the unknown
            // dimension based on the aspect ratio of sourceSize
            if (destSize.Width == 0)
            {
                destSize.Width = destSize.Height * aspect;
            }
            if (destSize.Height == 0)
            {
                destSize.Height = destSize.Width / aspect;
            }

            // Calculate target aspect ratio if needed (don't bother if resizeMode == scale to fill)
            nfloat targetAspect = 0.0f;
            if (resizeMode != RCTResizeMode.ScaleToFill)
            {
                targetAspect = destSize.Width / destSize.Height;
                if (aspect == targetAspect)
                {
                    resizeMode = RCTResizeMode.ScaleToFill;
                }
            }

            switch (resizeMode)
            {
                case RCTResizeMode.ScaleToFill:
                    return new CGRect(CGPoint.Empty, RCTCeilSize(destSize, destScale));

                case RCTResizeMode.ScaleAspectFit:
                    if (targetAspect <= aspect) // target is taller than content
                    {
                        sourceSize.Width = destSize.Width = destSize.Width;
                        sourceSize.Height = sourceSize.Width / aspect;

                    }
                    else // target is wider than content
                    {
                        sourceSize.Height = destSize.Height = destSize.Height;
                        sourceSize.Width = sourceSize.Height * aspect;
                    }

                    return new CGRect(
                        new CGPoint(
                            RCTFloorValue((destSize.Width - sourceSize.Width) / 2, destScale),
                            RCTFloorValue((destSize.Height - sourceSize.Height) / 2, destScale)
                        ),
                        RCTCeilSize(sourceSize, destScale)
                    );

                default:
                    if (targetAspect <= aspect)
                    { // target is taller than content

                        sourceSize.Height = destSize.Height = destSize.Height;
                        sourceSize.Width = sourceSize.Height * aspect;
                        destSize.Width = destSize.Height * targetAspect;
                        return new CGRect(
                            new CGPoint(
                                RCTFloorValue((destSize.Width - sourceSize.Width) / 2, destScale),
                                0
                            ),
                            RCTCeilSize(sourceSize, destScale)
                        );
                    }
                    else // target is wider than content
                    {
                        sourceSize.Width = destSize.Width = destSize.Width;
                        sourceSize.Height = sourceSize.Width / aspect;
                        destSize.Height = destSize.Width / targetAspect;
                        return new CGRect(
                            new CGPoint(
                                0,
                                RCTFloorValue((destSize.Height - sourceSize.Height) / 2, destScale)
                            ),
                            RCTCeilSize(sourceSize, destScale)
                        );
                    }
            }
        }

#if __IOS__
        static CGImage[] GetFrames(CGImageSource source, CGImageThumbnailOptions options)
        {
            var retval = new CGImage[source.ImageCount];

            for (int i = 0; i < source.ImageCount; i++)
            {
                var frameImage = source.CreateThumbnail(i, options);
                retval[i] = frameImage;
            }

            return retval;
        }


        /* The GIF stores a separate duration for each frame, in units of centiseconds (hundredths of a second).  However, a `UIImage` only has a single, total `duration` property, which is a floating-point number.
         * To handle this mismatch, I add each source image (from the GIF) to `animation` a varying number of times to match the ratios between the frame durations in the GIF.
         * For example, suppose the GIF contains three frames.  Frame 0 has duration 3.  Frame 1 has duration 9.  Frame 2 has duration 15.  I divide each duration by the greatest common denominator of all the durations,
         * which is 3, and add each frame the resulting number of times.  Thus `animation` will contain frame 0 3/3 = 1 time, then frame 1 9/3 = 3 times, then frame 2 15/3 = 5 times.
         * I set `animation.duration` to (3+9+15)/100 = 0.27 seconds. */
        static PImage[] AdjustFramesToSpoofDurations(CGImage[] images, nfloat scale, List<int> delays, int totalDuration)
        {
            var count = images.Length;
            var gcd = GetGCD(delays);
            var frameCount = totalDuration / 10 / gcd;
            var frames = new PImage[frameCount];
            var f = 0;

            for (var i = 0; i < count; i++)
            {
                var frame = PImage.FromImage(images[i], scale, UIImageOrientation.Up);
                for (var j = delays[i] / 10 / gcd; j > 0; --j)
                    frames[f++] = frame;
            }

            return frames.Where(v => v.CGImage != null).ToArray();
        }

        static int GetGCD(List<int> delays)
        {
            var gcd = delays[0] / 10;

            for (var i = 1; i < delays.Count; ++i)
                gcd = PairGCD(delays[i] / 10, gcd);

            return gcd;
        }

        static int PairGCD(int a, int b)
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
#endif
    }
}
