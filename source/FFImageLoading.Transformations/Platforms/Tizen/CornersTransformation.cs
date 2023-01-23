using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CornersTransformation : ITransformation
    {
        public CornersTransformation() : this(20d, CornerTransformType.TopRightRounded)
        {
        }

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType)
            : this(cornersSize, cornersSize, cornersSize, cornersSize, cornersTransformType, 1d, 1d)
        {
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType)
            : this(topLeftCornerSize, topRightCornerSize, bottomLeftCornerSize, bottomRightCornerSize, cornersTransformType, 1d, 1d)
        {
        }

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
            : this(cornersSize, cornersSize, cornersSize, cornersSize, cornersTransformType, cropWidthRatio, cropHeightRatio)
        {
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            TopLeftCornerSize = topLeftCornerSize;
            TopRightCornerSize = topRightCornerSize;
            BottomLeftCornerSize = bottomLeftCornerSize;
            BottomRightCornerSize = bottomRightCornerSize;
            CornersTransformType = cornersTransformType;
            CropWidthRatio = cropWidthRatio;
            CropHeightRatio = cropHeightRatio;
        }

        public double TopLeftCornerSize { get; set; }
        public double TopRightCornerSize { get; set; }
        public double BottomLeftCornerSize { get; set; }
        public double BottomRightCornerSize { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }
        public CornerTransformType CornersTransformType { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public string Key => $"CornersTransformation,cornersSizes={TopLeftCornerSize},{TopRightCornerSize},{BottomRightCornerSize},{BottomLeftCornerSize},cornersTransformType={CornersTransformType},cropWidthRatio={CropWidthRatio},cropHeightRatio={CropHeightRatio}";
    }
}