using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CropTransformation : ITransformation
    {
        public CropTransformation()
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public CropTransformation(double zoomFactor, double xOffset, double yOffset)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public CropTransformation(double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public double ZoomFactor { get; set; }
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }

        public string Key
        {
            get
            {
                throw new Exception(Common.DoNotReferenceMessage);
            }
        }
    }
}

