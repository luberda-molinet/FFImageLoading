using System;
using System.Globalization;
using Xamarin.Forms;

namespace FFImageLoading.Forms
{
    [Preserve(AllMembers = true)]
    public class ImageSourceConverter : TypeConverter, IValueConverter
    {
        public override bool CanConvertFrom(Type sourceType)
        {
            if (sourceType == null)
                throw new ArgumentNullException(nameof(sourceType));

            return sourceType == typeof(string);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConvertFromInvariantString(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ConvertFromInvariantString(string value)
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
    }
}

