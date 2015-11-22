using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace FFImageLoading
{
    public class FFImageSourceBinding
    {
        public FFImageSourceBinding(FFImageLoading.Work.ImageSource imageSource, string path)
        {
            ImageSource = imageSource;
            Path = path;
        }

        public FFImageLoading.Work.ImageSource ImageSource { get; private set; }

        public string Path { get; private set; }

        internal static async Task<FFImageSourceBinding> GetImageSourceBinding(string source)
        {
            if (source == null)
            {
                return null;
            }

            Uri uri;
            if (!Uri.TryCreate(source, UriKind.Absolute, out uri) || uri.Scheme == "file")
            {
                StorageFile file = null;

                try
                {
                    file = await StorageFile.GetFileFromPathAsync(source);
                }
                catch (Exception)
                {
                }

                if (file != null)
                {
                    return new FFImageSourceBinding(FFImageLoading.Work.ImageSource.Filepath, source);
                }

                return new FFImageSourceBinding(FFImageLoading.Work.ImageSource.CompiledResource, source);
            }

            return new FFImageSourceBinding(FFImageLoading.Work.ImageSource.Url, source);
        }

        public override bool Equals(object obj)
        {
            var item = obj as FFImageSourceBinding;

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
                return hash;
            }
        }
    }
}
