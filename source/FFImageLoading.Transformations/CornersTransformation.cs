using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CornersTransformation : ITransformation
    {
        public CornersTransformation()
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            throw new Exception(Common.DoNotReferenceMessage);
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
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public string Key
        {
            get
            {
                throw new Exception(Common.DoNotReferenceMessage);
            }
        }
    }
}

