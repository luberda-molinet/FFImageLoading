using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Transformations
{
    public class FlipTransformation : TransformationBase
    {
        private FlipType _flipType;

        public FlipTransformation(FlipType flipType)
        {
            _flipType = flipType;
        }

        public override string Key
        {
            get { return string.Format("FlipTransformation, Type=", _flipType.ToString()); }
        }

        protected override WriteableBitmap Transform(WriteableBitmap source)
        {
            return source;
        }
    }
}
