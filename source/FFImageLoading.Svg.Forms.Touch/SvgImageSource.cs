using System;
using System.IO;
using FFImageLoading.Forms;
using FFImageLoading.Work;
using FFImageLoading.Svg.Platform;

namespace FFImageLoading.Svg.Forms
{
	/// <summary>
	/// SVG image source.
	/// </summary>
	public class SvgImageSource : Xamarin.Forms.ImageSource, IVectorImageSource
	{
		public SvgImageSource(Xamarin.Forms.ImageSource imageSource, int vectorWidth, int vectorHeight, bool useDipUnits)
		{
			ImageSource = imageSource;
		}

		public Xamarin.Forms.ImageSource ImageSource { get; private set; }

		public int VectorWidth { get; set; } = 0;

		public int VectorHeight { get; set; } = 0;

		public bool UseDipUnits { get; set; } = true;

		public IVectorDataResolver GetVectorDataResolver()
		{
			return new SvgDataResolver(VectorWidth, VectorHeight, UseDipUnits);
		}

		/// <summary>
		/// SvgImageSource FromFile.
		/// By default it uses view size as vectorWidth / vectorHeight
		/// </summary>
		/// <returns>The file.</returns>
		/// <param name="file">File.</param>
		/// <param name="vectorWidth">Vector width.</param>
		/// <param name="vectorHeight">Vector height.</param>
		/// <param name="useDipUnits">If set to <c>true</c> use dip units.</param>
		public static SvgImageSource FromFile(string file, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true)
		{
			return new SvgImageSource(Xamarin.Forms.ImageSource.FromFile(file), vectorWidth, vectorHeight, useDipUnits);
		}

		/// <summary>
		/// SvgImageSource FromStream.
		/// By default it uses view size as vectorWidth / vectorHeight
		/// </summary>
		/// <returns>The stream.</returns>
		/// <param name="stream">Stream.</param>
		/// <param name="vectorWidth">Vector width.</param>
		/// <param name="vectorHeight">Vector height.</param>
		/// <param name="useDipUnits">If set to <c>true</c> use dip units.</param>
		public static SvgImageSource FromStream(Func<Stream> stream, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true)
		{
			return new SvgImageSource(Xamarin.Forms.ImageSource.FromStream(stream), vectorWidth, vectorHeight, useDipUnits);
		}

		/// <summary>
		/// SvgImageSource FromUri.
		/// By default it uses view size as vectorWidth / vectorHeight
		/// </summary>
		/// <returns>The URI.</returns>
		/// <param name="uri">URI.</param>
		/// <param name="vectorWidth">Vector width.</param>
		/// <param name="vectorHeight">Vector height.</param>
		/// <param name="useDipUnits">If set to <c>true</c> use dip units.</param>
		public static SvgImageSource FromUri(Uri uri, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true)
		{
			return new SvgImageSource(Xamarin.Forms.ImageSource.FromUri(uri), vectorWidth, vectorHeight, useDipUnits);
		}
	}
}
