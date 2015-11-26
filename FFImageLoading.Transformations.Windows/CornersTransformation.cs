using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Transformations
{
    public class CornersTransformation : TransformationBase
    {
        private double _topLeftCornerSize;
        private double _topRightCornerSize;
        private double _bottomLeftCornerSize;
        private double _bottomRightCornerSize;
        private double _cropWidthRatio;
        private double _cropHeightRatio;
        private CornerTransformType _cornersTransformType;

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType)
        {
            _topLeftCornerSize = cornersSize;
            _topRightCornerSize = cornersSize;
            _bottomLeftCornerSize = cornersSize;
            _bottomRightCornerSize = cornersSize;
            _cornersTransformType = cornersTransformType;
            _cropWidthRatio = 1f;
            _cropHeightRatio = 1f;
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType)
        {
            _topLeftCornerSize = topLeftCornerSize;
            _topRightCornerSize = topRightCornerSize;
            _bottomLeftCornerSize = bottomLeftCornerSize;
            _bottomRightCornerSize = bottomRightCornerSize;
            _cornersTransformType = cornersTransformType;
            _cropWidthRatio = 1f;
            _cropHeightRatio = 1f;
        }

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            _topLeftCornerSize = cornersSize;
            _topRightCornerSize = cornersSize;
            _bottomLeftCornerSize = cornersSize;
            _bottomRightCornerSize = cornersSize;
            _cornersTransformType = cornersTransformType;
            _cropWidthRatio = cropWidthRatio;
            _cropHeightRatio = cropHeightRatio;
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            _topLeftCornerSize = topLeftCornerSize;
            _topRightCornerSize = topRightCornerSize;
            _bottomLeftCornerSize = bottomLeftCornerSize;
            _bottomRightCornerSize = bottomRightCornerSize;
            _cornersTransformType = cornersTransformType;
            _cropWidthRatio = cropWidthRatio;
            _cropHeightRatio = cropHeightRatio;
        }

        public override string Key
        {
            get
            {
                return string.Format("CornersTransformation, cornersSizes = {0}/{1}/{2}/{3}, cornersTransformType = {4}, cropWidthRatio = {5}, cropHeightRatio = {6}, ",
              _topLeftCornerSize, _topRightCornerSize, _bottomRightCornerSize, _bottomLeftCornerSize, _cornersTransformType, _cropWidthRatio, _cropHeightRatio);
            }
        }

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            return source;
        }

        public static BitmapHolder ToTransformedCorners(BitmapHolder source, double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            double sourceWidth = source.Width;
            double sourceHeight = source.Height;

            double desiredWidth = sourceWidth;
            double desiredHeight = sourceHeight;

            double desiredRatio = cropWidthRatio / cropHeightRatio;
            double currentRatio = sourceWidth / sourceHeight;

            if (currentRatio > desiredRatio)
                desiredWidth = (cropWidthRatio * sourceHeight / cropHeightRatio);
            else if (currentRatio < desiredRatio)
                desiredHeight = (cropHeightRatio * sourceWidth / cropWidthRatio);

            topLeftCornerSize = topLeftCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            topRightCornerSize = topRightCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            bottomLeftCornerSize = bottomLeftCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            bottomRightCornerSize = bottomRightCornerSize * (desiredWidth + desiredHeight) / 2 / 100;

            float cropX = (float)((sourceWidth - desiredWidth) / 2);
            float cropY = (float)((sourceHeight - desiredHeight) / 2);

            //TODO not implemented

            return source;
        }
    }
}
