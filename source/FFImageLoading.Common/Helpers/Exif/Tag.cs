using System;

namespace FFImageLoading.Helpers.Exif
{
    public sealed class Tag
    {
        private readonly Directory _directory;

        internal Tag(int type, Directory directory)
        {
            Type = type;
            _directory = directory;
        }

        public int Type { get; }

        public string Value => _directory.GetValue(Type);

        public bool HasName => _directory.HasTagName(Type);

        public string Name => _directory.GetTagName(Type);

        public string DirectoryName => _directory.Name;
    }
}
