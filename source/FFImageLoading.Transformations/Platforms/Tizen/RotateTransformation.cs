using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RotateTransformation : ITransformation
    {
        public RotateTransformation() : this(30d)
        {
        }

        public RotateTransformation(double degrees) : this(degrees, false, false)
        {
        }

        public RotateTransformation(double degrees, bool ccw) : this(degrees, ccw, false)
        {
        }

        public RotateTransformation(double degrees, bool ccw, bool resize)
        {
            Degrees = degrees;
            CCW = ccw;
            Resize = resize;
        }

        public double Degrees { get; set; }
        public bool CCW { get; set; }
        public bool Resize { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public string Key => $"RotateTransformation,degrees={Degrees},ccw={CCW},resize={Resize}";
    }
}