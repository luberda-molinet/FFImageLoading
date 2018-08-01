using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class ColorFillTransformation : ITransformation
    {
        public ColorFillTransformation() : this("#000000")
        {
        }

        public ColorFillTransformation(string hexColor)
        {
            HexColor = hexColor;
        }

        public string HexColor { get; set; }

        public string Key => string.Format("ColorFillTransformation,hexColor={0}", HexColor);

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            throw new NotImplementedException();
        }
    }
}
