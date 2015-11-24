using FFImageLoading.Work;
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

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            return source;
        }
    }
}
