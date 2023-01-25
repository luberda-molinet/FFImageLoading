using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace FFImageLoading.Maui
{
    [Preserve(AllMembers = true)]
    public class ImageSourceConverter : TypeConverter, IValueConverter
    {
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
            if (sourceType == null)
                throw new ArgumentNullException(nameof(sourceType));

            return sourceType == typeof(string);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
			if (!(value is string text))
				return null;

			if (text.IsDataUrl())
			{
				return new DataUrlImageSource(text);
			}

			if (Uri.TryCreate(text, UriKind.Absolute, out var uri))
			{
				if (uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
					return ImageSource.FromFile(uri.LocalPath);
				if (uri.Scheme.Equals("resource", StringComparison.OrdinalIgnoreCase))
					return new EmbeddedResourceImageSource(uri);

				return ImageSource.FromUri(uri);
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				return ImageSource.FromFile(text);
			}

			throw new InvalidOperationException(string.Format("Cannot convert \"{0}\" into {1}", value, typeof(ImageSource)));
		}

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			return Convert(value, typeof(ImageSource), null, CultureInfo.CurrentCulture);
		}
	}
}

