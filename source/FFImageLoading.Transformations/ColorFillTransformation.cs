using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class ColorFillTransformation : ITransformation
    {
        public ColorFillTransformation()
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public ColorFillTransformation(string hexColor)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            throw new Exception(Common.DoNotReferenceMessage);
        }

        public string HexColor { get; set; }

        public string Key
        {
            get
            {
                throw new Exception(Common.DoNotReferenceMessage);
            }
        }
    }
}
