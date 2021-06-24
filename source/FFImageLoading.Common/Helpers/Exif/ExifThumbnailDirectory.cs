using System.Collections.Generic;

namespace FFImageLoading.Helpers.Exif
{
    internal class ExifThumbnailDirectory : ExifDirectoryBase
    {
        /// <summary>The offset to thumbnail image bytes.</summary>
        public const int TagThumbnailOffset = 0x0201;

        /// <summary>The size of the thumbnail image data in bytes.</summary>
        public const int TagThumbnailLength = 0x0202;

        public ExifThumbnailDirectory()
        {
            SetDescriptor(new ExifThumbnailDescriptor(this));
        }

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>();

        static ExifThumbnailDirectory()
        {
            AddExifTagNames(_tagNameMap);
        }

        public override string Name => "Exif Thumbnail";

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }
}
