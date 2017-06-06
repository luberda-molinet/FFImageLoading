using System;
using System.Globalization;
using Xamarin.Forms;
using System.Reflection;

namespace FFImageLoading.Forms
{
	public class ImageSourceConverter : TypeConverter
	{
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

            Uri uri;

            if (text != null && Uri.TryCreate(text, UriKind.Absolute, out uri))
            {
                if (uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
                    return ImageSource.FromFile(text);
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

