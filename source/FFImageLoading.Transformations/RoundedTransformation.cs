using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RoundedTransformation : ITransformation
    {
        public RoundedTransformation()
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public RoundedTransformation(double radius)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public double Radius { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }
        public double BorderSize { get; set; }
        public string BorderHexColor { get; set; }

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

