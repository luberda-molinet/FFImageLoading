using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FFImageLoading.Forms;
using FFImageLoading.Work;

namespace FFImageLoading.Svg.Forms
{
	/// <summary>
	/// SVG image source.
	/// </summary>
    [Preserve(AllMembers = true)]
	public class SvgImageSource : Xamarin.Forms.ImageSource, IVectorImageSource
	{
		const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform version";

        /// <summary>
        /// SvgImageSource
        /// </summary>
        /// <param name="imageSource"></param>
        /// <param name="vectorWidth"></param>
        /// <param name="vectorHeight"></param>
        /// <param name="useDipUnits"></param>
		public SvgImageSource(Xamarin.Forms.ImageSource imageSource, int vectorWidth, int vectorHeight, bool useDipUnits, Dictionary<string, string> replaceStringMap = null)
		{
			throw new Exception(DoNotReference);
		}

        /// <summary>
        /// ImageSource
        /// </summary>
		public Xamarin.Forms.ImageSource ImageSource { get; private set; }

        /// <summary>
        /// VectorWidth
        /// </summary>
		public int VectorWidth { get; set; } = 0;

        /// <summary>
        /// VectorHeight
        /// </summary>
		public int VectorHeight { get; set; } = 0;

        /// <summary>
        /// UseDipUnits
        /// </summary>
		public bool UseDipUnits { get; set; } = true;

        /// <summary>
        /// Gets or sets the replace string map. It can be used eg. to replace color strings inside SVG file
        /// </summary>
        /// <value>The replace string map.</value>
        public Dictionary<string, string> ReplaceStringMap { get; set; }

        /// <summary>
        /// GetVectorDataResolver
        /// </summary>
        /// <returns>IVectorDataResolver</returns>
		public IVectorDataResolver GetVectorDataResolver()
		{
			throw new Exception(DoNotReference);
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
		public static SvgImageSource FromFile(string file, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
		{
			throw new Exception(DoNotReference);
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
		public static SvgImageSource FromStream(Func<Stream> stream, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
		{
			throw new Exception(DoNotReference);
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
		public static SvgImageSource FromUri(Uri uri, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
		{
			throw new Exception(DoNotReference);
		}

        /// <summary>
		/// SvgImageSource FromResource.
		/// By default it uses view size as vectorWidth / vectorHeight
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="resolvingType"></param>
        /// <param name="vectorWidth"></param>
        /// <param name="vectorHeight"></param>
        /// <param name="useDipUnits"></param>
        /// <returns></returns>
		public static SvgImageSource FromResource(string resource, Type resolvingType, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
		{
			throw new Exception(DoNotReference);
		}

        /// <summary>
		/// SvgImageSource FromResource.
		/// By default it uses view size as vectorWidth / vectorHeight
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="sourceAssembly"></param>
        /// <param name="vectorWidth"></param>
        /// <param name="vectorHeight"></param>
        /// <param name="useDipUnits"></param>
        /// <returns></returns>
		public static SvgImageSource FromResource(string resource, Assembly sourceAssembly = null, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
		{
			throw new Exception(DoNotReference);
		}

		/// <summary>
		/// SvgImageSource FromSvgString.
		/// </summary>
		/// <returns>The svg string.</returns>
		/// <param name="svg">Svg.</param>
		/// <param name="vectorWidth">Vector width.</param>
		/// <param name="vectorHeight">Vector height.</param>
		/// <param name="useDipUnits">If set to <c>true</c> use dip units.</param>
		/// <param name="replaceStringMap">Replace string map.</param>
		public static SvgImageSource FromSvgString(string svg, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
        {
            throw new Exception(DoNotReference);
        }
	}
}
