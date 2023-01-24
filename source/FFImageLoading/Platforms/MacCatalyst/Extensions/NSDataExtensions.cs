using System;
using Foundation;
using CoreGraphics;
using ImageIO;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using FFImageLoading.Config;
using System.Threading;
using FFImageLoading.Decoders;
using System.Linq;
using System.Threading.Tasks;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif

namespace FFImageLoading.Extensions
{
    public static class NSDataExtensions
    {
        public static async Task<PImage> ToImageAsync(this NSData data, CGSize destSize, nfloat destScale, Configuration config, TaskParameter parameters, GifDecoder.RCTResizeMode resizeMode = GifDecoder.RCTResizeMode.ScaleAspectFit, ImageInformation imageinformation = null, bool allowUpscale = false)
        {
            var decoded = await GifDecoder.SourceRegfToDecodedImageAsync(
				data, destSize, destScale, config, parameters, resizeMode, imageinformation, allowUpscale).ConfigureAwait(false);

            PImage result;

            if (decoded.IsAnimated)
            {
#if __IOS__
                    result = PImage.CreateAnimatedImage(decoded.AnimatedImages
                                                        .Select(v => v.Image)
                                                        .Where(v => v?.CGImage != null).ToArray(), decoded.AnimatedImages.Sum(v => v.Delay) / 100.0);
#elif __MACOS__
                result = new PImage();
                var repr = decoded.AnimatedImages
                                  .Select(v => v.Image.Representations().First())
                                  .ToArray();
                result.AddRepresentations(repr);
#endif
            }
            else
            {
                result = decoded.Image;
            }

            return result;
        }
    }
}

