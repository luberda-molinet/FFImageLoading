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
		}

		public Configuration Configuration { get { return ImageService.Instance.Config; } }

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
											.Resolve(identifier, parameters, token).ConfigureAwait(false);

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

			float sizeX = 0;
			float sizeY = 0;

			if (VectorWidth == 0 && VectorHeight == 0)
			{
				sizeX = 200;
				sizeY = (VectorWidth / picture.Bounds.Width) * picture.Bounds.Height;
			}
			else if (VectorWidth > 0 && VectorHeight > 0)
			{
				sizeX = VectorWidth;
				sizeY = VectorHeight;
			}
			else if (VectorWidth > 0)
			{
				sizeX = VectorWidth;
				sizeY = (VectorWidth / picture.Bounds.Width) * picture.Bounds.Height;
			}
			else
			{
				sizeX = (VectorHeight / picture.Bounds.Height) * picture.Bounds.Width;
				sizeY = VectorHeight;
			}

			using (var bitmap = new SKBitmap((int)sizeX, (int)sizeY))
			using (var canvas = new SKCanvas(bitmap))
			using (var paint = new SKPaint())
			{
				canvas.Clear(SKColors.Transparent);
				float scaleX = sizeX / picture.Bounds.Width;
				float scaleY = sizeY / picture.Bounds.Height;
				var matrix = SKMatrix.MakeScale(scaleX, scaleY);

				canvas.DrawPicture(picture, ref matrix, paint);
				canvas.Flush();

				using (var image = SKImage.FromBitmap(bitmap))
				using (var data = image.Encode(SKImageEncodeFormat.Png, 80))
				{
					var stream = new MemoryStream();
					data.SaveTo(stream);
					stream.Position = 0;
					//var stream = data?.AsStream();
					return new Tuple<Stream, LoadingResult, ImageInformation>(stream, resolvedData.Item2, resolvedData.Item3);
				}
			}
		}
	}
}
