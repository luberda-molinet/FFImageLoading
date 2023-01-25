using System;
using System.IO;
using FFImageLoading.Maui;
using FFImageLoading.Work;
using FFImageLoading.Svg.Platform;
using System.Reflection;
using System.Collections.Generic;
using FFImageLoading.Config;
using Microsoft.Extensions.DependencyInjection;

namespace FFImageLoading.Svg.Maui
{
    [Preserve(AllMembers = true)]
    /// <summary>
    /// SVG image source.
    /// </summary>
    public class SvgImageSource : Microsoft.Maui.Controls.ImageSource, IVectorImageSource
    {
        public SvgImageSource(Microsoft.Maui.Controls.ImageSource imageSource, int vectorWidth, int vectorHeight, bool useDipUnits, Dictionary<string, string> replaceStringMap = null)
        {
            ImageSource = imageSource;
            VectorWidth = vectorWidth;
            VectorHeight = vectorHeight;
            UseDipUnits = useDipUnits;
            ReplaceStringMap = replaceStringMap;
        }

        public Microsoft.Maui.Controls.ImageSource ImageSource { get; private set; }

        public int VectorWidth { get; set; }

        public int VectorHeight { get; set; }

        public bool UseDipUnits { get; set; }

        public Dictionary<string, string> ReplaceStringMap { get; set; }

        public IVectorDataResolver GetVectorDataResolver()
        {
			var imageService = this.FindMauiContext()?.Services?.GetRequiredService<IImageService<TImageContainer>>();

            return new SvgDataResolver<TImageContainer>(imageService, VectorWidth, VectorHeight, UseDipUnits, ReplaceStringMap);
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
        /// <param name="replaceStringMap">Replace string map.</param>
        public static SvgImageSource FromFile(string file, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
        {
            return new SvgImageSource(Microsoft.Maui.Controls.ImageSource.FromFile(file), vectorWidth, vectorHeight, useDipUnits, replaceStringMap);
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
        /// <param name="replaceStringMap">Replace string map.</param>
        public static SvgImageSource FromStream(Func<Stream> stream, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
        {
            return new SvgImageSource(Microsoft.Maui.Controls.ImageSource.FromStream(stream), vectorWidth, vectorHeight, useDipUnits, replaceStringMap);
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
        /// <param name="replaceStringMap">Replace string map.</param>
        public static SvgImageSource FromUri(Uri uri, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
        {
            return new SvgImageSource(Microsoft.Maui.Controls.ImageSource.FromUri(uri), vectorWidth, vectorHeight, useDipUnits, replaceStringMap);
        }

        /// <summary>
        /// SvgImageSource FromResource.
        /// By default it uses view size as vectorWidth / vectorHeight
        /// </summary>
        /// <returns>The resource.</returns>
        /// <param name="resource">Resource.</param>
        /// <param name="resolvingType">Resolving type.</param>
        /// <param name="vectorWidth">Vector width.</param>
        /// <param name="vectorHeight">Vector height.</param>
        /// <param name="useDipUnits">If set to <c>true</c> use dip units.</param>
        /// <param name="replaceStringMap">Replace string map.</param>
        public static SvgImageSource FromResource(string resource, Type resolvingType, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
        {

            return FromResource(resource, resolvingType.GetTypeInfo().Assembly, vectorWidth, vectorHeight, useDipUnits, replaceStringMap);
        }

        /// <summary>
        /// SvgImageSource FromResource.
        /// By default it uses view size as vectorWidth / vectorHeight
        /// </summary>
        /// <returns>The resource.</returns>
        /// <param name="resource">Resource.</param>
        /// <param name="sourceAssembly">Source assembly.</param>
        /// <param name="vectorWidth">Vector width.</param>
        /// <param name="vectorHeight">Vector height.</param>
        /// <param name="useDipUnits">If set to <c>true</c> use dip units.</param>
        /// <param name="replaceStringMap">Replace string map.</param>
        public static SvgImageSource FromResource(string resource, Assembly sourceAssembly = null, int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
        {
            if (sourceAssembly == null)
            {
                MethodInfo callingAssemblyMethod = typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetCallingAssembly");
                if (callingAssemblyMethod != null)
                {
                    sourceAssembly = (Assembly)callingAssemblyMethod.Invoke(null, new object[0]);
                }
                else
                {
                    return null;
                }
            }

            return new SvgImageSource(new EmbeddedResourceImageSource(resource, sourceAssembly), vectorWidth, vectorHeight, useDipUnits, replaceStringMap);
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
            return new SvgImageSource(new DataUrlImageSource(svg), vectorWidth, vectorHeight, useDipUnits, replaceStringMap);
        }

		public IVectorImageSource Clone()
		{
			return new SvgImageSource(ImageSource, VectorWidth, VectorHeight, UseDipUnits, ReplaceStringMap);
		}
	}
}
