using System;
using System.Collections.Generic;
using FFImageLoading.Forms;
using Xamarin.Forms;

namespace FFImageLoading.Svg.Forms
{
#if __IOS__
            [Foundation.Preserve(AllMembers = true)]
#elif __ANDROID__
            [Android.Runtime.Preserve(AllMembers = true)]
#endif

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

        protected override ImageSource CoerceImageSource(object newValue)
        {
            var source = base.CoerceImageSource(newValue);;

            var fileSource = source as FileImageSource;
            if (fileSource?.File != null)
            {
                if (fileSource.File.StartsWith("<", StringComparison.OrdinalIgnoreCase))
                {
                    return new SvgImageSource(new DataUrlImageSource(fileSource.File), 0, 0, true, ReplaceStringMap);
                }
                else if (fileSource.File.IsSvgFileUrl())
                {
                    return new SvgImageSource(fileSource, 0, 0, true, ReplaceStringMap);
                }
            }

            var uriSource = source as UriImageSource;
            if (uriSource?.Uri?.OriginalString != null)
            {
                if (uriSource.Uri.OriginalString.IsSvgDataUrl())
                {
                    return new SvgImageSource(uriSource, 0, 0, true, ReplaceStringMap);
                }
                else if (uriSource.Uri.OriginalString.IsSvgFileUrl())
                {
                    return new SvgImageSource(uriSource, 0, 0, true, ReplaceStringMap);
                }
            }

            var dataUrlSource = source as DataUrlImageSource;
            if (dataUrlSource?.DataUrl != null)
            {
                if (dataUrlSource.DataUrl.IsSvgDataUrl())
                {
                    return new SvgImageSource(dataUrlSource, 0, 0, true, ReplaceStringMap);
                }
            }

            var embeddedSource = source as EmbeddedResourceImageSource;
            if (embeddedSource?.Uri?.OriginalString != null)
            {
                if (embeddedSource.Uri.OriginalString.IsSvgFileUrl())
                {
                    return new SvgImageSource(embeddedSource, 0, 0, true, ReplaceStringMap);
                }
            }

            return source;
        }

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
            get
            {
                return (Dictionary<string, string>)GetValue(ReplaceStringMapProperty);
            }
            set
            {

                SetValue(ReplaceStringMapProperty, value);
            }
        }

        static void HandleReplaceStringMapPropertyChangedDelegate(BindableObject bindable, object oldValue, object newValue)
        {
            var cachedImage = bindable as SvgCachedImage;
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

            if (ReplaceStringMap != null)
            {
                var source = imageLoader.CustomDataResolver as Work.IVectorDataResolver;
                if (source != null && source.ReplaceStringMap == null)
                    source.ReplaceStringMap = ReplaceStringMap;

                var loadingSource = imageLoader.CustomLoadingPlaceholderDataResolver as Work.IVectorDataResolver;
                if (loadingSource != null && loadingSource.ReplaceStringMap == null)
                    loadingSource.ReplaceStringMap = ReplaceStringMap;

                var errorSource = imageLoader.CustomErrorPlaceholderDataResolver as Work.IVectorDataResolver;
                if (errorSource != null && errorSource.ReplaceStringMap == null)
                    errorSource.ReplaceStringMap = ReplaceStringMap;
            }
        }
    }
}
