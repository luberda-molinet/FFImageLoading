using System;
using FFImageLoading.Work;
using FFImageLoading.Extensions;

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

        public static ColorHolder BlendColor(ColorHolder color, ColorHolder backColor)
        {
            float amount = (float)color.A / 255;

            byte r = (byte)((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backColor.B * (1 - amount));

            return new ColorHolder(r, g, b);
        }

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, ImageSource source, bool isPlaceholder, string key)
        {
            var len = bitmapSource.PixelCount;
            var backColor = HexColor.ToColorFromHex();

            for (var i = 0; i < len; i++)
            {
                var color = bitmapSource.GetPixel(i);
                bitmapSource.SetPixel(i, BlendColor(color, backColor));
            }

            return bitmapSource;
        }
    }
}
