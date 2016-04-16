using System;
using Xamarin.Forms;
using System.Globalization;

namespace FFImageLoading.Forms
{
	public class ImageSourceConverter : TypeConverter
	{
		public override bool CanConvertFrom(Type sourceType)
		{
			return sourceType == typeof(string);
		}

		public override object ConvertFrom(CultureInfo culture, object value)
		{
			if (value == null)
			{
				return null;
			}

			string text = value as string;
			if (text == null)
			{
				throw new InvalidOperationException(string.Format("Cannot convert {0} into {1}", new object[] {
					value,
					typeof(ImageSource)
				}));
			}

			Uri uri;
			if (!Uri.TryCreate(text, UriKind.Absolute, out uri) || uri.Scheme == "file")
			{
				return ImageSource.FromFile(text);
			}

			return ImageSource.FromUri(uri);
		}
	}
}

