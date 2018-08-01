using System;
using Android.Graphics;
using System.Linq;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class ColorSpaceTransformation : TransformationBase
    {
        ColorMatrix _colorMatrix;
        float[][] _rgbawMatrix;

        public ColorSpaceTransformation() : this(FFColorMatrix.InvertColorMatrix)
        {
        }

        public ColorSpaceTransformation(float[][] rgbawMatrix)
        {
            if (rgbawMatrix.Length != 5 || rgbawMatrix.Any(v => v.Length != 5))
                throw new ArgumentException("Wrong size of RGBAW color matrix");

            _colorMatrix = new ColorMatrix();
            RGBAWMatrix = rgbawMatrix;
        }

        public ColorSpaceTransformation(ColorMatrix colorMatrix)
        {
            _colorMatrix = colorMatrix;
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
                UpdateColorMatrix(_rgbawMatrix);
            }
        }

        public override string Key
        {
            get
            {
                if (_rgbawMatrix == null)
                    return string.Format("ColorSpaceTransformation,colorMatrix={0}",
                        string.Join(",", _colorMatrix.GetArray()));

                return string.Format("ColorSpaceTransformation,rgbawMatrix={0}",
                    string.Join(",", _rgbawMatrix.Select(x => string.Join(",", x)).ToArray()));
            }
        }

        void UpdateColorMatrix(float[][] rgbawMatrix)
        {
            var rOffset = rgbawMatrix[0][4];
            var gOffset = rgbawMatrix[1][4];
            var bOffset = rgbawMatrix[2][4];
            var aOffset = rgbawMatrix[3][4];

            _colorMatrix.SetScale(rOffset, gOffset, bOffset, aOffset);
            var transposed = GetAndroidMatrix(rgbawMatrix);
            _colorMatrix.Set(transposed);
        }

        static float[] GetAndroidMatrix(float[][] rgbawMatrix)
        {
            var transposed = new float[20];
            int counter = 0;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    transposed[counter] = rgbawMatrix[j][i];
                    counter++;
                }
            }

            return transposed;
        }

        protected override Bitmap Transform(Bitmap sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToColorSpace(sourceBitmap, _colorMatrix);
        }

        public static Bitmap ToColorSpace(Bitmap source, ColorMatrix colorMatrix)
        {
            var config = source.GetConfig();
                    if (config == null)
                        config = Bitmap.Config.Argb8888;    // This will support transparency

            int width = source.Width;
            int height = source.Height;

            Bitmap bitmap = Bitmap.CreateBitmap(width, height, config);

            using (Canvas canvas = new Canvas(bitmap))
            using (Paint paint = new Paint())
            {
                paint.SetColorFilter(new ColorMatrixColorFilter(colorMatrix));
                canvas.DrawBitmap(source, 0, 0, paint);

                return bitmap;
            }
        }
    }
}

