using System;
using System.Globalization;
using Xamarin.Forms;

namespace FFImageLoading.Svg.Forms
{
    /// <summary>
    /// SvgImageSourceConverter
    /// </summary>
	public class SvgImageSourceConverter : IValueConverter
	{
		ImageSourceConverter imageSourceConverter = new ImageSourceConverter();

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

			var xfSource = imageSourceConverter.ConvertFromInvariantString(str) as ImageSource;

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
	}
}
