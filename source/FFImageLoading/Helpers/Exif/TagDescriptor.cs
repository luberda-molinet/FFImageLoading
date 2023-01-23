using System;

namespace FFImageLoading.Helpers.Exif
{
    internal class TagDescriptor<T> : ITagDescriptor where T : Directory
    {
        protected readonly T Directory;

        public TagDescriptor(T directory)
        {
            Directory = directory;
        }

        public virtual string GetDescription(int tagType)
        {
            var obj = Directory.GetObject(tagType);
            if (obj == null)
                return null;

            // special presentation for long arrays
            if (obj is Array array && array.Length > 16)
                return $"[{array.Length} {(array.Length == 1 ? "value" : "values")}]";

            // no special handling required, so use default conversion to a string
            return Directory.GetString(tagType);
        }
    }
}
