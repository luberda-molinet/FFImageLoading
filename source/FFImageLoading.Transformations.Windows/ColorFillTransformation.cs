using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class ColorFillTransformation : TransformationBase
    {
        public ColorFillTransformation() : this("#000000")
        {
        }

        public ColorFillTransformation(string hexColor)
        {
            HexColor = hexColor;
        }

        public string HexColor { get; set; }

        public override string Key => string.Format("ColorFillTransformation,hexColor={0}", HexColor);

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            throw new NotImplementedException();
        }
    }
}
