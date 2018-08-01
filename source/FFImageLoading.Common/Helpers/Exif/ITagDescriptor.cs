using System;

namespace FFImageLoading.Helpers.Exif
{
    internal interface ITagDescriptor
    {
        string GetDescription(int tagType);
    }
}
