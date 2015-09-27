using System;
using Xamarin.Forms;

namespace FFImageLoading.Forms.Touch
{
	internal class ImageSourceBinding
	{
		public ImageSourceBinding(FFImageLoading.Work.ImageSource imageSource, string path)
		{
			ImageSource = imageSource;
			Path = path;
		}

		public FFImageLoading.Work.ImageSource ImageSource { get; private set; }

		public string Path { get; private set; }

		internal static ImageSourceBinding GetImageSourceBinding(ImageSource source)
		{
			if (source == null)
			{
				return null;
			}

			var uriImageSource = source as UriImageSource;
			if (uriImageSource != null)
			{
				var urlSource = uriImageSource;
				return new ImageSourceBinding(FFImageLoading.Work.ImageSource.Url, urlSource.Uri.ToString());
			}

			var fileImageSource = source as FileImageSource;
			if (fileImageSource != null)
			{
				var fileSource = fileImageSource;
				return new ImageSourceBinding(FFImageLoading.Work.ImageSource.ApplicationBundle, fileSource.File);
			}

			throw new NotImplementedException("ImageSource type not supported");
		}
	}
}

