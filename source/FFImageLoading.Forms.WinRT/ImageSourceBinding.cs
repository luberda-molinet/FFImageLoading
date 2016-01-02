using System;
using Xamarin.Forms;
using Windows.Storage;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

#if WINDOWS_UWP
namespace FFImageLoading.Forms.WinUWP
#elif SILVERLIGHT
namespace FFImageLoading.Forms.WinSL
#else
namespace FFImageLoading.Forms.WinRT
#endif
{
    public class ImageSourceBinding
    {
        public ImageSourceBinding(FFImageLoading.Work.ImageSource imageSource, string path)
        {
            ImageSource = imageSource;
            Path = path;
        }

        public ImageSourceBinding(Func<CancellationToken, Task<Stream>> stream)
        {
            ImageSource = FFImageLoading.Work.ImageSource.Stream;
            Stream = stream;
        }

        public FFImageLoading.Work.ImageSource ImageSource { get; private set; }

        public string Path { get; private set; }

        public Func<CancellationToken, Task<Stream>> Stream { get; private set; }

        internal static async Task<ImageSourceBinding> GetImageSourceBinding(Xamarin.Forms.ImageSource source)
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
                StorageFile file = null;

                try
                {
                    var filePath = System.IO.Path.GetDirectoryName(fileImageSource.File);

                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
						file = await StorageFile.GetFileFromPathAsync(fileImageSource.File);
                    }
                }
                catch (Exception)
                {
                }

                if (file != null)
                {
                    return new ImageSourceBinding(FFImageLoading.Work.ImageSource.Filepath, fileImageSource.File);
                }

                return new ImageSourceBinding(FFImageLoading.Work.ImageSource.ApplicationBundle, fileImageSource.File);
            }

            var streamImageSource = source as StreamImageSource;
            if (streamImageSource != null)
            {
                return new ImageSourceBinding(streamImageSource.Stream);
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

            return this.ImageSource.Equals(item.ImageSource) && this.Path.Equals(item.Path) && this.Stream.Equals(item.Stream);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.ImageSource.GetHashCode();
                hash = hash * 23 + Path.GetHashCode();
                hash = hash * 23 + Stream.GetHashCode();
                return hash;
            }
        }
    }
}
