using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers=true)]
    public class BlurredTransformation : ITransformation
    {
        public BlurredTransformation()
        {
            Radius = 25d;
        }

        public BlurredTransformation(double radius)
        {
            Radius = radius;
        }

        public double Radius { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public string Key => $"BlurredTransformation,radius={Radius}";
    }
}