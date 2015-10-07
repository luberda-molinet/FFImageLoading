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
				return new ImageSourceBinding(FFImageLoading.Work.ImageSource.Url, uriImageSource.Uri.ToString());
			}

			var fileImageSource = source as FileImageSource;
			if (fileImageSource != null)
			{
				return new ImageSourceBinding(FFImageLoading.Work.ImageSource.ApplicationBundle, fileImageSource.File);
			}

			throw new NotImplementedException("ImageSource type not supported");
		}

		public override bool Equals(object obj)
		{
			var item = obj as ImageSourceBinding;

			if (item == null)
			{
				return false;
			}

			return this.ImageSource.Equals(item.ImageSource) && this.Path.Equals(item.Path);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + this.ImageSource.GetHashCode();
				hash = hash * 23 + Path.GetHashCode();
				return  hash;
			}
		}
	}
}

