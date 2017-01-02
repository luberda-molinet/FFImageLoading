using System;
using System.Globalization;
using Xamarin.Forms;

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

			if (text != null)
			{
				Uri uri;
				return Uri.TryCreate(text, UriKind.Absolute, out uri) && uri.Scheme != "file" ? ImageSource.FromUri(uri) : ImageSource.FromFile(text);
			}

			throw new InvalidOperationException(string.Format("Cannot convert \"{0}\" into {1}", value, typeof(ImageSource)));
		}
	}
}

