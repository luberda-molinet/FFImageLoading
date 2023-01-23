using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RoundedTransformation : ITransformation
    {
        public RoundedTransformation() : this(30d)
        {
        }

        public RoundedTransformation(double radius) : this(radius, 1d, 1d)
        {
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio) : this(radius, cropWidthRatio, cropHeightRatio, 0d, null)
        {
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
        {
            Radius = radius;
            CropWidthRatio = cropWidthRatio;
            CropHeightRatio = cropHeightRatio;
            BorderSize = borderSize;
            BorderHexColor = borderHexColor;
        }

        public double Radius { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }
        public double BorderSize { get; set; }
        public string BorderHexColor { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public string Key => $"RoundedTransformation,radius={Radius},cropWidthRatio={CropWidthRatio},cropHeightRatio={CropHeightRatio},borderSize={BorderSize},borderHexColor={BorderHexColor}";
    }
}

