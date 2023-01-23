using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CropTransformation : ITransformation
    {
        public CropTransformation() : this(1d, 0d, 0d)
        {
        }

        public CropTransformation(double zoomFactor, double xOffset, double yOffset) : this(zoomFactor, xOffset, yOffset, 1f, 1f)
        {
        }

        public CropTransformation(double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
        {
            ZoomFactor = zoomFactor;
            XOffset = xOffset;
            YOffset = yOffset;
            CropWidthRatio = cropWidthRatio;
            CropHeightRatio = cropHeightRatio;

            if (ZoomFactor < 1f)
                ZoomFactor = 1f;
        }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public double ZoomFactor { get; set; }
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }

        public string Key => $"CropTransformation,zoomFactor={ZoomFactor},xOffset={XOffset},yOffset={YOffset},cropWidthRatio={CropWidthRatio},cropHeightRatio={CropHeightRatio}";
    }
}