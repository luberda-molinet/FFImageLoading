using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Transformations
{
    public class CircleTransformation : TransformationBase
    {
        public CircleTransformation()
        {
        }

        public override string Key
        {
            get { return "CircleTransformation"; }
        }

        protected override WriteableBitmap Transform(WriteableBitmap source)
        {
            return source;
        }
    }
}
