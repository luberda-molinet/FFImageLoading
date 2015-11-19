using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Transformations
{
    public class ColorSpaceTransformation : TransformationBase
    {
        public ColorSpaceTransformation(float[][] rgbawMatrix)
        {
            if (rgbawMatrix.Length != 5 || rgbawMatrix.Any(v => v.Length != 5))
                throw new ArgumentException("Wrong size of RGBAW color matrix");
        }

        public override string Key
        {
            get { return "ColorSpaceTransformation"; }
        }

        protected override WriteableBitmap Transform(WriteableBitmap source)
        {
            return source;
        }
    }
}
