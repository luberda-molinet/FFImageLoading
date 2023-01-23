using System;
using System.Text;

namespace FFImageLoading.Helpers.Exif
{
    internal class ExifIfd0Descriptor : ExifDescriptorBase<ExifIfd0Directory>
    {
        public ExifIfd0Descriptor(ExifIfd0Directory directory) : base(directory)
        {
        }
    }

    internal abstract class ExifDescriptorBase<T> : TagDescriptor<T> where T : Directory
    {
        protected ExifDescriptorBase(T directory) : base(directory)
        {
        }

        public override string GetDescription(int tagType)
        {
            return base.GetDescription(tagType);
        }
    }
}
