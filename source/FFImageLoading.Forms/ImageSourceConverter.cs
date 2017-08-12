using System;
using System.Globalization;
using Xamarin.Forms;
using System.Reflection;

namespace FFImageLoading.Forms
{
    [Preserve(AllMembers = true)]
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

            if (text.IsDataUrl())
            {
                return new DataUrlImageSource(text);
            }

            Uri uri;

            if (Uri.TryCreate(text, UriKind.Absolute, out uri))
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

