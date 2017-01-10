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
	/// <summary>
	/// Svg data resolver.
	/// </summary>
	public class SvgDataResolver : IVectorDataResolver
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FFImageLoading.Svg.Platform.SvgDataResolver"/> class.
		/// Default SVG size is read from SVG file width / height attributes
		/// You can override it by specyfing vectorWidth / vectorHeight params
		/// </summary>
		/// <param name="vectorWidth">Vector width.</param>
		/// <param name="vectorHeight">Vector height.</param>
		/// <param name="useDipUnits">If set to <c>true</c> use dip units.</param>
		public SvgDataResolver(int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true)
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
				if (picture.Bounds.Width > 0)
					sizeX = picture.Bounds.Width;
				else
					sizeX = 300;

				if (picture.Bounds.Height > 0)
					sizeY = picture.Bounds.Height;
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
