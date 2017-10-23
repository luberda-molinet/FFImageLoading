using System;
using System.Globalization;
using FFImageLoading.Forms;
using Xamarin.Forms;

namespace FFImageLoading.Svg.Forms
{
    /// <summary>
    /// SvgImageSourceConverter
    /// </summary>
    [Preserve(AllMembers = true)]
    public class SvgImageSourceConverter : FFImageLoading.Forms.ImageSourceConverter, IValueConverter
    {
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

            return ConvertFromInvariantString(str);
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

        public override object ConvertFromInvariantString(string value)
        {
            var text = value as string;
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var xfSource = base.ConvertFromInvariantString(value) as ImageSource;

            if (text.IsSvgFileUrl() || text.IsSvgDataUrl())
            {
                return new SvgImageSource(xfSource, 0, 0, true);
            }

            return xfSource;
        }
    }
}
