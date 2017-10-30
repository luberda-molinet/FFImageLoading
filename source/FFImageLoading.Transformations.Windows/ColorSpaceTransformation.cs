using FFImageLoading.Work;
using System;
using System.Linq;

namespace FFImageLoading.Transformations
{
    public class ColorSpaceTransformation : TransformationBase
    {
        float[][] _rgbawMatrix;

        public ColorSpaceTransformation() : this(FFColorMatrix.InvertColorMatrix)
        {
        }

        public ColorSpaceTransformation(float[][] rgbawMatrix)
        {
            if (rgbawMatrix.Length != 5 || rgbawMatrix.Any(v => v.Length != 5))
                throw new ArgumentException("Wrong size of RGBAW color matrix");

            RGBAWMatrix = rgbawMatrix;
        }

        public float[][] RGBAWMatrix
        {
            get
            {
                return _rgbawMatrix;
            }

            set
            {
                if (value.Length != 5 || value.Any(v => v.Length != 5))
                    throw new ArgumentException("Wrong size of RGBAW color matrix");

                _rgbawMatrix = value;
            }
        }

        public override string Key
        {
            get
            {
                return string.Format("ColorSpaceTransformation,rgbawMatrix={0}",
                    string.Join(",", _rgbawMatrix.Select(x => string.Join(",", x)).ToArray()));
            }
        }

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            ToColorSpace(bitmapSource, _rgbawMatrix);

            return bitmapSource;
        }

        public static void ToColorSpace(BitmapHolder bmp, float[][] rgbawMatrix)
        {
            var r0 = rgbawMatrix[0][0];
            var r1 = rgbawMatrix[0][1];
            var r2 = rgbawMatrix[0][2];
            var r3 = rgbawMatrix[0][3];

            var g0 = rgbawMatrix[1][0];
            var g1 = rgbawMatrix[1][1];
            var g2 = rgbawMatrix[1][2];
            var g3 = rgbawMatrix[1][3];

            var b0 = rgbawMatrix[2][0];
            var b1 = rgbawMatrix[2][1];
            var b2 = rgbawMatrix[2][2];
            var b3 = rgbawMatrix[2][3];

            var a0 = rgbawMatrix[3][0];
            var a1 = rgbawMatrix[3][1];
            var a2 = rgbawMatrix[3][2];
            var a3 = rgbawMatrix[3][3];

            var rOffset = rgbawMatrix[4][0];
            var gOffset = rgbawMatrix[4][1];
            var bOffset = rgbawMatrix[4][2];
            var aOffset = rgbawMatrix[4][3];

            var nWidth = bmp.Width;
            var nHeight = bmp.Height;
            var len = bmp.PixelCount;

            for (var i = 0; i < len; i++)
            {
                var color = bmp.GetPixel(i);
                var a = color.A;
                var r = color.R;
                var g = color.G;
                var b = color.B;

                var rNew = (int)(r * r0 + g * g0 + b * b0 + a * a0 + rOffset);
                var gNew = (int)(r * r1 + g * g1 + b * b1 + a * a1 + gOffset);
                var bNew = (int)(r * r2 + g * g2 + b * b2 + a * a2 + bOffset);
                var aNew = (int)(r * r3 + g * g3 + b * b3 + a * a3 + aOffset);

                bmp.SetPixel(i, new ColorHolder(aNew, rNew, gNew, bNew));
            }
        }
    }
}
