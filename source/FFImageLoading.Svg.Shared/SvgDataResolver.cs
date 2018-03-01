using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;
using FFImageLoading.Config;
using SkiaSharp;
using FFImageLoading.DataResolvers;
using FFImageLoading.Extensions;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

#if __IOS__
using Foundation;
using UIKit;
using CoreGraphics;
#elif __ANDROID__
using Android.Util;
using Android.Runtime;
using Android.Content;
#endif

namespace FFImageLoading.Svg.Platform
{
    /// <summary>
    /// Svg data resolver.
    /// </summary>
#if __IOS__
    [Preserve(AllMembers = true)]
#elif __ANDROID__
    [Preserve(AllMembers = true)]
#endif
    public class SvgDataResolver : IVectorDataResolver
    {
        static readonly object _encodingLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FFImageLoading.Svg.Platform.SvgDataResolver"/> class.
        /// Default SVG size is read from SVG file width / height attributes
        /// You can override it by specyfing vectorWidth / vectorHeight params
        /// </summary>
        /// <param name="vectorWidth">Vector width.</param>
        /// <param name="vectorHeight">Vector height.</param>
        /// <param name="useDipUnits">If set to <c>true</c> use dip units.</param>
        /// <param name="replaceStringMap">Replace string map.</param>
        public SvgDataResolver(int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
        {
            VectorWidth = vectorWidth;
            VectorHeight = vectorHeight;
            UseDipUnits = useDipUnits;
            ReplaceStringMap = replaceStringMap;
        }

        public Configuration Configuration { get { return ImageService.Instance.Config; } }

        public bool UseDipUnits { get; private set; }

        public int VectorHeight { get; private set; }

        public int VectorWidth { get; private set; }

        public Dictionary<string, string> ReplaceStringMap { get; set; }

        public async Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            ImageSource source = parameters.Source;

            if (!string.IsNullOrWhiteSpace(parameters.LoadingPlaceholderPath) && parameters.LoadingPlaceholderPath == identifier)
                source = parameters.LoadingPlaceholderSource;
            else if (!string.IsNullOrWhiteSpace(parameters.ErrorPlaceholderPath) && parameters.ErrorPlaceholderPath == identifier)
                source = parameters.ErrorPlaceholderSource;

            var resolvedData = await (Configuration.DataResolverFactory ?? new DataResolverFactory())
                                            .GetResolver(identifier, source, parameters, Configuration)
                                            .Resolve(identifier, parameters, token).ConfigureAwait(false);

            if (resolvedData?.Stream == null)
                throw new FileNotFoundException(identifier);

            var svg = new SKSvg()
            {
                ThrowOnUnsupportedElement = false,
            };
            SKPicture picture;

            if (ReplaceStringMap == null || ReplaceStringMap.Count == 0)
            {
                using (var svgStream = resolvedData.Stream)
                {
                    picture = svg.Load(svgStream);
                }
            }
            else
            {
                using (var svgStream = resolvedData.Stream)
                using (var reader = new StreamReader(svgStream))
                {
                    var inputString = await reader.ReadToEndAsync();

                    foreach (var map in ReplaceStringMap
                             .Where(v => v.Key.StartsWith("regex:")))
                    {
                        inputString = Regex.Replace(inputString, map.Key.Substring(0, 6), map.Value);
                    }

                    var builder = new StringBuilder(inputString);

                    foreach (var map in ReplaceStringMap
                             .Where(v => !v.Key.StartsWith("regex:", StringComparison.OrdinalIgnoreCase)))
                    {
                        builder.Replace(map.Key, map.Value);
                    }

                    using (var svgFinalStream = new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString())))
                    {
                        picture = svg.Load(svgFinalStream);
                    }
                }
            }

            double sizeX = 0;
            double sizeY = 0;

            if (VectorWidth <= 0 && VectorHeight <= 0)
            {
                if (picture.CullRect.Width > 0)
                    sizeX = picture.CullRect.Width;
                else
                    sizeX = 300;

                if (picture.CullRect.Height > 0)
                    sizeY = picture.CullRect.Height;
                else
                    sizeY = 300;
            }
            else if (VectorWidth > 0 && VectorHeight > 0)
            {
                sizeX = VectorWidth;
                sizeY = VectorHeight;
            }
            else if (VectorWidth > 0)
            {
                sizeX = VectorWidth;
                sizeY = (VectorWidth / picture.CullRect.Width) * picture.CullRect.Height;
            }
            else
            {
                sizeX = (VectorHeight / picture.CullRect.Height) * picture.CullRect.Width;
                sizeY = VectorHeight;
            }

            if (UseDipUnits)
            {
                sizeX = sizeX.DpToPixels();
                sizeY = sizeY.DpToPixels();
            }

            lock (_encodingLock)
            {
                using (var bitmap = new SKBitmap(new SKImageInfo((int)sizeX, (int)sizeY)))
                //using (var bitmap = new SKBitmap((int)sizeX, (int)sizeY))
                using (var canvas = new SKCanvas(bitmap))
                using (var paint = new SKPaint())
                {
                    canvas.Clear(SKColors.Transparent);
                    float scaleX = (float)sizeX / picture.CullRect.Width;
                    float scaleY = (float)sizeY / picture.CullRect.Height;
                    var matrix = SKMatrix.MakeScale(scaleX, scaleY);

                    canvas.DrawPicture(picture, ref matrix, paint);
                    canvas.Flush();

                    using (var image = SKImage.FromBitmap(bitmap))
                    //using (var data = image.Encode(SKImageEncodeFormat.Png, 100))  //TODO disabled because of https://github.com/mono/SkiaSharp/issues/285
                    using (var data = image.Encode())
                    {
                        var stream = new MemoryStream();
                        data.SaveTo(stream);
                        stream.Position = 0;
                        resolvedData.ImageInformation.SetType(ImageInformation.ImageType.SVG);
                        return new DataResolverResult(stream, resolvedData.LoadingResult, resolvedData.ImageInformation);
                    }
                }
            }
        }
    }
}
