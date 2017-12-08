using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class FlipTransformation : ITransformation
    {
        public FlipTransformation() : this(FlipType.Horizontal)
        {
        }

        public FlipTransformation(FlipType flipType)
        {
            FlipType = flipType;
        }

        public FlipType FlipType { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }
        public string Key => $"FlipTransformation,Type={FlipType}";
    }
}