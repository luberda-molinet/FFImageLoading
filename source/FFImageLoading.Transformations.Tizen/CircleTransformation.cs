using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CircleTransformation : ITransformation
    {
        public CircleTransformation()
        {
        }

        public CircleTransformation(double borderSize, string borderHexColor)
        {
            BorderSize = borderSize;
            BorderHexColor = borderHexColor;
        }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public double BorderSize { get; set; }
        public string BorderHexColor { get; set; }

        public string Key => $"CircleTransformation,borderSize={BorderSize},borderHexColor={BorderHexColor}";
    }
}