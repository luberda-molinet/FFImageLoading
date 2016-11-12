using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;
using FFImageLoading.Config;
using SkiaSharp;
using FFImageLoading.DataResolvers;

namespace FFImageLoading.Svg.Platform
{
	public class SvgDataResolver : IVectorDataResolver
	{
		public SvgDataResolver(int vectorWidth, int vectorHeight, bool useDipUnits)
		{
			VectorWidth = vectorWidth;
			VectorHeight = vectorHeight;
			UseDipUnits = useDipUnits;
			Configuration = ImageService.Instance.Config;
		}

		public Configuration Configuration { get; private set; }

		public bool UseDipUnits { get; private set; }

		public int VectorHeight { get; private set; }

		public int VectorWidth { get; private set; }

		public async Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
		{
			ImageSource source = parameters.Source;

			if (!string.IsNullOrWhiteSpace(parameters.LoadingPlaceholderPath) && parameters.LoadingPlaceholderPath == identifier)
				source = parameters.LoadingPlaceholderSource;
			else if (!string.IsNullOrWhiteSpace(parameters.ErrorPlaceholderPath) && parameters.ErrorPlaceholderPath == identifier)
				source = parameters.ErrorPlaceholderSource;

			var resolvedData = await (Configuration.DataResolverFactory ?? new DataResolverFactory())
			                                .GetResolver(identifier, source, parameters, Configuration)
			                                .Resolve(identifier, parameters, token);

			if (resolvedData?.Item1 == null)
				throw new FileNotFoundException(identifier);

			var svg = new SKSvg()
			{
				ThrowOnUnsupportedElement = false,
			};
			SKPicture picture;

			using (var svgStream = resolvedData.Item1)
			{
				picture = svg.Load(resolvedData?.Item1);
			}

			using (var bitmap = new SKBitmap(100, 100, true))
			using (var canvas = new SKCanvas(bitmap))
			{
				//float canvasMin = Math.Min(200, 200);
				//float svgMax = Math.Max(svg.Picture.Bounds.Width, svg.Picture.Bounds.Height);
				//float scale = canvasMin / svgMax;
				//var matrix = SKMatrix.MakeScale(scale, scale);
				//canvas.DrawPicture(picture, ref matrix);
				canvas.DrawPicture(picture);
				canvas.Flush();
				using (var image = SKImage.FromBitmap(bitmap))
				using (var data = image.Encode(SKImageEncodeFormat.Png, 80))
				{
					var stream = data?.AsStream();
					return new Tuple<Stream, LoadingResult, ImageInformation>(stream, resolvedData.Item2, resolvedData.Item3);
				}
			}
		}
	}
}
