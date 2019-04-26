using System;
using System.Collections.Generic;
using FFImageLoading.Forms;
using Xamarin.Forms;

namespace FFImageLoading.Svg.Forms
{
    [Preserve(AllMembers = true)]
    /// <summary>
    /// SvgCachedImage by Daniel Luberda
    /// </summary>
    public class SvgCachedImage : CachedImage
    {
        public static void Init()
        {
            var ignore = typeof(SvgCachedImage);
        }

        /// <summary>
        /// SvgCachedImage by Daniel Luberda
        /// </summary>
        public SvgCachedImage() : base()
        {
            ReplaceStringMap = new Dictionary<string, string>();
        }

		private class AutoSvgImageSource : SvgImageSource
		{
			public AutoSvgImageSource(ImageSource imageSource, int vectorWidth, int vectorHeight, bool useDipUnits, Dictionary<string, string> replaceStringMap = null)
				: base (imageSource, vectorWidth, vectorHeight, useDipUnits, replaceStringMap)
			{

			}
		}

		protected override ImageSource CoerceImageSource(object newValue)
        {
            var source = base.CoerceImageSource(newValue); ;

            var fileSource = source as FileImageSource;
            if (fileSource?.File != null)
            {
                if (fileSource.File.StartsWith("<", StringComparison.OrdinalIgnoreCase))
                {
                    return new AutoSvgImageSource(new DataUrlImageSource(fileSource.File), 0, 0, true, ReplaceStringMap);
                }
                else if (fileSource.File.IsSvgFileUrl())
                {
                    return new AutoSvgImageSource(fileSource, 0, 0, true, ReplaceStringMap);
                }
            }

            var uriSource = source as UriImageSource;
            if (uriSource?.Uri?.OriginalString != null)
            {
                if (uriSource.Uri.OriginalString.IsSvgDataUrl())
                {
                    return new AutoSvgImageSource(uriSource, 0, 0, true, ReplaceStringMap);
                }
                else if (uriSource.Uri.OriginalString.IsSvgFileUrl())
                {
                    return new AutoSvgImageSource(uriSource, 0, 0, true, ReplaceStringMap);
                }
            }

            var dataUrlSource = source as DataUrlImageSource;
            if (dataUrlSource?.DataUrl != null)
            {
                if (dataUrlSource.DataUrl.IsSvgDataUrl())
                {
                    return new AutoSvgImageSource(dataUrlSource, 0, 0, true, ReplaceStringMap);
                }
            }

            var embeddedSource = source as EmbeddedResourceImageSource;
            if (embeddedSource?.Uri?.OriginalString != null)
            {
                if (embeddedSource.Uri.OriginalString.IsSvgFileUrl())
                {
                    return new AutoSvgImageSource(embeddedSource, 0, 0, true, ReplaceStringMap);
                }
            }

            return source;
        }

        internal bool ReplaceStringMapHasChanged { get; set;}

		/// <summary>
		/// The error placeholder property.
		/// </summary>
		public static readonly BindableProperty ReplaceStringMapProperty = BindableProperty.Create(nameof(ReplaceStringMap), typeof(Dictionary<string, string>), typeof(SvgCachedImage), default(Dictionary<string, string>), propertyChanged: HandleReplaceStringMapPropertyChangedDelegate);

        /// <summary>
        /// Used to define replacement map which will be used to
        /// replace text inside SVG file (eg. changing colors values)
        /// </summary>
        /// <value>The replace string map.</value>
        public Dictionary<string, string> ReplaceStringMap
        {
            get => (Dictionary<string, string>)GetValue(ReplaceStringMapProperty);
            set => SetValue(ReplaceStringMapProperty, value);
        }

        static void HandleReplaceStringMapPropertyChangedDelegate(BindableObject bindable, object oldValue, object newValue)
        {
            var cachedImage = (SvgCachedImage)bindable;
			cachedImage.ReplaceStringMapHasChanged = true;
			cachedImage?.ReloadImage();
        }

        /// <summary>
        /// Setups the on before image loading.
        /// You can add additional logic here to configure image loader settings before loading
        /// </summary>
        /// <param name="imageLoader">Image loader.</param>
        protected override void SetupOnBeforeImageLoading(Work.TaskParameter imageLoader)
        {
            base.SetupOnBeforeImageLoading(imageLoader);

            if (ReplaceStringMapHasChanged)
            {
                ReplaceStringMapHasChanged = false;
                
                var source = imageLoader.CustomDataResolver as Work.IVectorDataResolver;
                if (source != null && (Source is AutoSvgImageSource || source.ReplaceStringMap == null))
                    source.ReplaceStringMap = ReplaceStringMap;

                var loadingSource = imageLoader.CustomLoadingPlaceholderDataResolver as Work.IVectorDataResolver;
                if (loadingSource != null && (LoadingPlaceholder is AutoSvgImageSource || loadingSource.ReplaceStringMap == null))
                    loadingSource.ReplaceStringMap = ReplaceStringMap;

                var errorSource = imageLoader.CustomErrorPlaceholderDataResolver as Work.IVectorDataResolver;
                if (errorSource != null && (ErrorPlaceholder is AutoSvgImageSource || errorSource.ReplaceStringMap == null))
                    errorSource.ReplaceStringMap = ReplaceStringMap;
            }
        }
    }
}
