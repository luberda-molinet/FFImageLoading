using System;
using System.Globalization;
using FFImageLoading.Forms;
using Xamarin.Forms;

namespace FFImageLoading.Svg.Forms
{
	/// <summary>
	/// SvgImageSourceConverter
	/// </summary>
#if __IOS__
            [Foundation.Preserve(AllMembers = true)]
#elif __ANDROID__
            [Android.Runtime.Preserve(AllMembers = true)]
#endif
    [Preserve(AllMembers = true)]
	public class SvgImageSourceConverter : TypeConverter, IValueConverter
	{
        FFImageLoading.Forms.ImageSourceConverter _imageSourceConverter = new FFImageLoading.Forms.ImageSourceConverter();

        /// <summary>
        /// Convert
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var str = value as string;
			if (string.IsNullOrWhiteSpace(str))
				return null;

			var xfSource = _imageSourceConverter.ConvertFromInvariantString(str) as ImageSource;

			//TODO Parse width / height eg. image.svg@SVG=0x200  where 200 is width
			return new SvgImageSource(xfSource, 0, 0, true);
		}

        /// <summary>
        /// ConvertBack
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

        public override bool CanConvertFrom(Type sourceType)
        {
        	return sourceType == typeof(string);
        }

        [Obsolete]
        public override object ConvertFrom(CultureInfo culture, object value)
        {
			var text = value as string;

			if (text == null)
				return null;

            var xfSource = _imageSourceConverter.ConvertFromInvariantString(text) as ImageSource;

            if (text.Contains("svg", StringComparison.OrdinalIgnoreCase))
            {
                return new SvgImageSource(xfSource, 0, 0, true);
            }

            return xfSource;
        }

        public override object ConvertFromInvariantString(string value)
        {
			var text = value as string;

			if (text == null)
				return null;

			var xfSource = _imageSourceConverter.ConvertFromInvariantString(text) as ImageSource;

			if (text.Contains("svg", StringComparison.OrdinalIgnoreCase))
			{
				return new SvgImageSource(xfSource, 0, 0, true);
			}

			return xfSource;
        }
	}
}
