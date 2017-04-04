using System;
using FFImageLoading.Forms;
using Xamarin.Forms;

namespace FFImageLoading.Svg.Forms
{
    public class SvgCachedImage : CachedImage
    {
        public SvgCachedImage() : base()
        {
        }

        /// <summary>
        /// The source property.
        /// </summary> 
        public static new readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(SvgCachedImage), default(ImageSource), BindingMode.OneWay, propertyChanging: OnSourcePropertyChanging);

        static void OnSourcePropertyChanging(BindableObject bindable, object oldValue, object newValue)
        {
            var element = (CachedImage)bindable;
            element.Source = newValue as ImageSource;
        }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        [TypeConverter(typeof(SvgImageSourceConverter))]
        public new ImageSource Source
        {
        	get
        	{
        		return (ImageSource)GetValue(SourceProperty);
        	}
        	set
        	{
        		SetValue(SourceProperty, value);
        	}
        }

        /// <summary>
        /// The loading placeholder property.
        /// </summary>
        public static new readonly BindableProperty LoadingPlaceholderProperty = BindableProperty.Create(nameof(LoadingPlaceholder), typeof(ImageSource), typeof(SvgCachedImage), default(ImageSource), propertyChanging: OnLoadingPlaceholderPropertyChanging);

        static void OnLoadingPlaceholderPropertyChanging(BindableObject bindable, object oldValue, object newValue)
        {
        	var element = (CachedImage)bindable;
            element.LoadingPlaceholder = newValue as ImageSource;
        }

        /// <summary>
        /// Gets or sets the loading placeholder image.
        /// </summary>
        [TypeConverter(typeof(SvgImageSourceConverter))]
        public new ImageSource LoadingPlaceholder
        {
        	get
        	{
        		return (ImageSource)GetValue(LoadingPlaceholderProperty);
        	}
        	set
        	{
        		SetValue(LoadingPlaceholderProperty, value);
        	}
        }

        /// <summary>
        /// The error placeholder property.
        /// </summary>
        public static new readonly BindableProperty ErrorPlaceholderProperty = BindableProperty.Create(nameof(ErrorPlaceholder), typeof(ImageSource), typeof(SvgCachedImage), default(ImageSource), propertyChanging: OnErrorPlaceholderPropertyChanging);

        static void OnErrorPlaceholderPropertyChanging(BindableObject bindable, object oldValue, object newValue)
        {
        	var element = (CachedImage)bindable;
        	element.ErrorPlaceholder = newValue as ImageSource;
        }

        /// <summary>
        /// Gets or sets the error placeholder image.
        /// </summary>
        [TypeConverter(typeof(SvgImageSourceConverter))]
        public new ImageSource ErrorPlaceholder
        {
        	get
        	{
        		return (ImageSource)GetValue(ErrorPlaceholderProperty);
        	}
        	set
        	{
        		SetValue(ErrorPlaceholderProperty, value);
        	}
        }
    }
}
