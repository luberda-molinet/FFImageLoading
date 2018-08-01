using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RotateTransformation : ITransformation
    {
        public RotateTransformation()
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public RotateTransformation(double degrees) : this(degrees, false, false)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public RotateTransformation(double degrees, bool ccw) : this(degrees, ccw, false)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public RotateTransformation(double degrees, bool ccw, bool resize)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public double Degrees { get; set; }
        public bool CCW { get; set; }
        public bool Resize { get; set; }

        public string Key
        {
            get
            {
                throw new Exception(Common.DoNotReferenceMessage);
            }
        }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }
    }
}

