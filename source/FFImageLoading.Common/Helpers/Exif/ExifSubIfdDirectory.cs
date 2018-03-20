using System;
using System.Collections.Generic;

namespace FFImageLoading.Helpers.Exif
{
    internal class ExifSubIfdDirectory : ExifDirectoryBase
    {
        /// <summary>This tag is a pointer to the Exif Interop IFD.</summary>
        public const int TagInteropOffset = 0xA005;

        public ExifSubIfdDirectory()
        {
            SetDescriptor(new ExifSubIfdDescriptor(this));
        }

        private static readonly Dictionary<int, string> _tagNameMap = new Dictionary<int, string>();

        static ExifSubIfdDirectory()
        {
            AddExifTagNames(_tagNameMap);
        }

        public override string Name => "Exif SubIFD";

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            return _tagNameMap.TryGetValue(tagType, out tagName);
        }
    }
}
