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
using FFImageLoading.Helpers;

#if __IOS__
using Foundation;
using UIKit;
using CoreGraphics;
#elif __MACOS__
using Foundation;
using AppKit;
using CoreGraphics;
#elif __ANDROID__
using Android.Util;
using Android.Runtime;
using Android.Content;
using Android.Graphics;
using FFImageLoading.Drawables;
#elif __WINDOWS__
using Windows.Foundation;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace FFImageLoading.Svg.Platform
{
    /// <summary>
    /// Svg data resolver.
    /// </summary>
    [Preserve(AllMembers = true)]
    public class SvgDataResolver : IVectorDataResolver
    {
#pragma warning disable RECS0108 // Warns about static fields in generic types
		private static readonly object _encodingLock = new object();
#pragma warning restore RECS0108 // Warns about static fields in generic types

#if __IOS__
		private static readonly CGColorSpace _colorSpace = CGColorSpace.CreateDeviceRGB();
#elif __MACOS__
		private static readonly CGColorSpace _colorSpace = CGColorSpace.CreateDeviceRGB();
#endif

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

        public Configuration Configuration => ImageService.Instance.Config;

        public bool UseDipUnits { get; private set; }

        public int VectorHeight { get; private set; }

        public int VectorWidth { get; private set; }

        public Dictionary<string, string> ReplaceStringMap { get; set; }

		private async Task<DataResolverResult> Decode(SKPicture picture, SKBitmap bitmap, DataResolverResult resolvedData)
		{
			await StaticLocks.DecodingLock.WaitAsync().ConfigureAwait(false);

			try
			{
#if __IOS__
                var info = bitmap.Info;            
				using (var provider = new CGDataProvider(bitmap.GetPixels(out var size), size.ToInt32()))
				using (var cgImage = new CGImage(info.Width, info.Height, 8, info.BitsPerPixel, info.RowBytes,
						_colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big,
						provider, null, false, CGColorRenderingIntent.Default))
				{
					IDecodedImage<object> container = new DecodedImage<object>()
					{
						Image = new UIImage(cgImage),
					};

					return new DataResolverResult(container, resolvedData.LoadingResult, resolvedData.ImageInformation);
				}
#elif __MACOS__
                var info = bitmap.Info;
				using (var provider = new CGDataProvider(bitmap.GetPixels(out var size), size.ToInt32()))
				using (var cgImage = new CGImage(info.Width, info.Height, 8, info.BitsPerPixel, info.RowBytes,
						_colorSpace, CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big,
						provider, null, false, CGColorRenderingIntent.Default))
				{
					IDecodedImage<object> container = new DecodedImage<object>()
					{
						Image = new NSImage(cgImage, CGSize.Empty),
					};
					return new DataResolverResult(container, resolvedData.LoadingResult, resolvedData.ImageInformation);
				}
#elif __ANDROID__
				using (var skiaPixmap = bitmap.PeekPixels())
				{
					var info = skiaPixmap.Info;

					// destination values
					var config = Bitmap.Config.Argb8888;
					var dstInfo = new SKImageInfo(info.Width, info.Height);

					// try keep the pixel format if we can
					switch (info.ColorType)
					{
						case SKColorType.Alpha8:
							config = Bitmap.Config.Alpha8;
							dstInfo.ColorType = SKColorType.Alpha8;
							break;
						case SKColorType.Rgb565:
							config = Bitmap.Config.Rgb565;
							dstInfo.ColorType = SKColorType.Rgb565;
							dstInfo.AlphaType = SKAlphaType.Opaque;
							break;
						case SKColorType.Argb4444:
							config = Bitmap.Config.Argb4444;
							dstInfo.ColorType = SKColorType.Argb4444;
							break;
					}

					// destination bitmap
					var bmp = Bitmap.CreateBitmap(info.Width, info.Height, config);
					var ptr = bmp.LockPixels();

					// copy
					var success = skiaPixmap.ReadPixels(dstInfo, ptr, dstInfo.RowBytes);

					// confirm
					bmp.UnlockPixels();
					if (!success)
					{
						bmp.Recycle();
						bmp.Dispose();
						bmp = null;
					}

					IDecodedImage<object> container = new DecodedImage<object>()
					{
						Image = bmp,
					};
					return new DataResolverResult(container, resolvedData.LoadingResult, resolvedData.ImageInformation);
				}
#elif __WINDOWS__
                //var pixels = bitmap.Pixels;
                //for (int i = 0; i < pixels.Length; i++)
                //{
                //	int bytePos = i * 4;
                //	var color = pixels[i];

                //	pixelData[bytePos] = color.Blue;
                //	pixelData[bytePos + 1] = color.Green;
                //	pixelData[bytePos + 2] = color.Red;
                //	pixelData[bytePos + 3] = color.Alpha;
                //}

                byte[] pixelData = new byte[bitmap.Width * bitmap.Height * 4];
                System.Runtime.InteropServices.Marshal.Copy(bitmap.GetPixels(), pixelData, 0, bitmap.Width * bitmap.Height * 4);

                IDecodedImage<object> container = new DecodedImage<object>()
                {
                    Image = new BitmapHolder(pixelData, bitmap.Width, bitmap.Height),
                };

                return new DataResolverResult(container, resolvedData.LoadingResult, resolvedData.ImageInformation);
#else
				lock (_encodingLock)
				{
					using (var image = SKImage.FromBitmap(bitmap))
					//using (var data = image.Encode(SKImageEncodeFormat.Png, 100))  //TODO disabled because of https://github.com/mono/SkiaSharp/issues/285
					using (var data = image.Encode())
					{
						var stream = new MemoryStream();
						data.SaveTo(stream);
						stream.Position = 0;
						return new DataResolverResult(stream, resolvedData.LoadingResult, resolvedData.ImageInformation);
					}
				}
#endif
			}
			finally
			{
				StaticLocks.DecodingLock.Release();
			}
		}

		public async Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var source = parameters.Source;

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
                    picture = svg.Load(svgStream, token);
                }
            }
            else
            {
                using (var svgStream = resolvedData.Stream)
                using (var reader = new StreamReader(svgStream))
                {
                    var inputString = await reader.ReadToEndAsync().ConfigureAwait(false);

                    foreach (var map in ReplaceStringMap
                             .Where(v => v.Key.StartsWith("regex:", StringComparison.OrdinalIgnoreCase)))
                    {
                        inputString = Regex.Replace(inputString, map.Key.Substring(6), map.Value);
                    }

                    var builder = new StringBuilder(inputString);

                    foreach (var map in ReplaceStringMap
                             .Where(v => !v.Key.StartsWith("regex:", StringComparison.OrdinalIgnoreCase)))
                    {
                        builder.Replace(map.Key, map.Value);
                    }

					token.ThrowIfCancellationRequested();

					using (var svgFinalStream = new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString())))
                    {
                        picture = svg.Load(svgFinalStream);
                    }
                }
            }

			token.ThrowIfCancellationRequested();

            double sizeX = VectorWidth;
            double sizeY = VectorHeight;

			if (UseDipUnits)
			{
				sizeX = VectorWidth.DpToPixels();
				sizeY = VectorHeight.DpToPixels();
			}

			if (sizeX <= 0 && sizeY <= 0)
            {
                if (picture.CullRect.Width > 0)
                    sizeX = picture.CullRect.Width;
                else
                    sizeX = 400;

                if (picture.CullRect.Height > 0)
                    sizeY = picture.CullRect.Height;
                else
                    sizeY = 400;
            }
            else if (sizeX > 0 && sizeY <= 0)
            {
                sizeY = (int)(sizeX / picture.CullRect.Width * picture.CullRect.Height);
            }
            else if (sizeX <= 0 && sizeY > 0)
			{
                sizeX = (int)(sizeY / picture.CullRect.Height * picture.CullRect.Width);
            }

            resolvedData.ImageInformation.SetType(ImageInformation.ImageType.SVG);

            using (var bitmap = new SKBitmap(new SKImageInfo((int)sizeX, (int)sizeY)))
            using (var canvas = new SKCanvas(bitmap))
            using (var paint = new SKPaint())
            {
                canvas.Clear(SKColors.Transparent);
                var scaleX = (float)sizeX / picture.CullRect.Width;
                var scaleY = (float)sizeY / picture.CullRect.Height;
                var matrix = SKMatrix.MakeScale(scaleX, scaleY);
                canvas.DrawPicture(picture, ref matrix, paint);
                canvas.Flush();

				token.ThrowIfCancellationRequested();

				return await Decode(picture, bitmap, resolvedData).ConfigureAwait(false);
			}
        }
    }
}
