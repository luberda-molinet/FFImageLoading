using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Transformations
{
    public class RoundedTransformation : TransformationBase
    {
        private double _radius;
        private double _cropWidthRatio;
        private double _cropHeightRatio;

        public RoundedTransformation(double radius)
        {
            _radius = radius;
            _cropWidthRatio = 1f;
            _cropHeightRatio = 1f;
        }

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio)
        {
            _radius = radius;
            _cropWidthRatio = cropWidthRatio;
            _cropHeightRatio = cropHeightRatio;
        }

        public override string Key
        {
            get
            {
                return string.Format("RoundedTransformation, radius = {0}, cropWidthRatio = {1}, cropHeightRatio = {2}",
              _radius, _cropWidthRatio, _cropHeightRatio);
            }
        }

        protected override WriteableBitmap Transform(WriteableBitmap source)
        {
            return source;
        }
    }
}
