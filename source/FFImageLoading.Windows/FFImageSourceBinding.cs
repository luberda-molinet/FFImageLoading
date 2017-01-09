using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace FFImageLoading
{
    /// <summary>
    /// This class optimizes the call to "StorageFile.GetFileFromPathAsync" that is time consuming.
    /// The source of each image is the key of the cache... once a source has been checked the first time, any other control can be skipped 
    /// </summary>
    public static class FFImageSourceBindingCheckerCache
    {
        private static Dictionary<string, bool> _cache = new Dictionary<string, bool>();

        public static async Task<bool> IsThisFile(string source)
        {

            if (_cache.ContainsKey(source))
            {
                return _cache[source];
            }
            else
            {
                StorageFile file = null;
                try
                {
                    var filePath = System.IO.Path.GetDirectoryName(source);
                    if (!string.IsNullOrWhiteSpace(filePath) && !(filePath.TrimStart('\\', '/')).StartsWith("Assets"))
                    {
                        file = await StorageFile.GetFileFromPathAsync(source);
                    }
                }
                catch (Exception)
                {
                }
                _cache.Add(source, file != null);
                return file != null;
            }
        }
    }

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
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            Uri uri;
            if (!Uri.TryCreate(source, UriKind.Absolute, out uri) || uri.Scheme == "file")
            {
                var isFile = await FFImageSourceBindingCheckerCache.IsThisFile(source);
                if (isFile)
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
