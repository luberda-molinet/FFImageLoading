using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace FFImageLoading
{
    public class FFImageSourceBinding
    {
        public FFImageSourceBinding(Work.ImageSource imageSource, string path)
        {
            ImageSource = imageSource;
            Path = path;
        }

        public Work.ImageSource ImageSource { get; private set; }

        public string Path { get; private set; }

        internal static async Task<FFImageSourceBinding> GetImageSourceBinding(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            Uri uri;
            if (!Uri.TryCreate(source, UriKind.Absolute, out uri) || uri.Scheme == "file")
            {
                var isFile = await Cache.FFSourceBindingCache.IsFileAsync(source);
                if (isFile)
                {
                    return new FFImageSourceBinding(Work.ImageSource.Filepath, source);
                }

                return new FFImageSourceBinding(Work.ImageSource.CompiledResource, source);
            }

            return new FFImageSourceBinding(Work.ImageSource.Url, source);
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
