using System;
using Android.Graphics;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CornersTransformation : TransformationBase
    {
        public CornersTransformation() : this(20d, CornerTransformType.TopRightRounded)
        {
        }

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType)
            : this(cornersSize, cornersSize, cornersSize, cornersSize, cornersTransformType, 1d, 1d)
        {
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType)
            : this(topLeftCornerSize, topRightCornerSize, bottomLeftCornerSize, bottomRightCornerSize, cornersTransformType, 1d, 1d)
        {
        }

        public CornersTransformation(double cornersSize, CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
            : this(cornersSize, cornersSize, cornersSize, cornersSize, cornersTransformType, cropWidthRatio, cropHeightRatio)
        {
        }

        public CornersTransformation(double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            TopLeftCornerSize = topLeftCornerSize;
            TopRightCornerSize = topRightCornerSize;
            BottomLeftCornerSize = bottomLeftCornerSize;
            BottomRightCornerSize = bottomRightCornerSize;
            CornersTransformType = cornersTransformType;
            CropWidthRatio = cropWidthRatio;
            CropHeightRatio = cropHeightRatio;
        }

        public double TopLeftCornerSize { get; set; }
        public double TopRightCornerSize { get; set; }
        public double BottomLeftCornerSize { get; set; }
        public double BottomRightCornerSize { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }
        public CornerTransformType CornersTransformType { get; set; }

        public override string Key
        {
            get { return string.Format("CornersTransformation,cornersSizes={0},{1},{2},{3},cornersTransformType={4},cropWidthRatio={5},cropHeightRatio={6},",
                TopLeftCornerSize, TopRightCornerSize, BottomRightCornerSize, BottomLeftCornerSize, CornersTransformType, CropWidthRatio, CropHeightRatio); }
        }

        protected override Bitmap Transform(Bitmap sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToTransformedCorners(sourceBitmap, TopLeftCornerSize, TopRightCornerSize, BottomLeftCornerSize, BottomRightCornerSize,
                CornersTransformType, CropWidthRatio, CropHeightRatio);
        }

        public static Bitmap ToTransformedCorners(Bitmap source, double topLeftCornerSize, double topRightCornerSize, double bottomLeftCornerSize, double bottomRightCornerSize,
            CornerTransformType cornersTransformType, double cropWidthRatio, double cropHeightRatio)
        {
            double sourceWidth = source.Width;
            double sourceHeight = source.Height;

            double desiredWidth = sourceWidth;
            double desiredHeight = sourceHeight;

            double desiredRatio = cropWidthRatio / cropHeightRatio;
            double currentRatio = sourceWidth / sourceHeight;

            if (currentRatio > desiredRatio)
            {
                desiredWidth = (cropWidthRatio * sourceHeight / cropHeightRatio);
            }
            else if (currentRatio < desiredRatio)
            {
                desiredHeight = (cropHeightRatio * sourceWidth / cropWidthRatio);
            }

            topLeftCornerSize = topLeftCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            topRightCornerSize = topRightCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            bottomLeftCornerSize = bottomLeftCornerSize * (desiredWidth + desiredHeight) / 2 / 100;
            bottomRightCornerSize = bottomRightCornerSize * (desiredWidth + desiredHeight) / 2 / 100;

            float cropX = (float)((sourceWidth - desiredWidth) / 2);
            float cropY = (float)((sourceHeight - desiredHeight) / 2);

            Bitmap bitmap = Bitmap.CreateBitmap((int)desiredWidth, (int)desiredHeight, Bitmap.Config.Argb8888);
            bitmap.HasAlpha = true;

            using (Canvas canvas = new Canvas(bitmap))
            using (Paint paint = new Paint())
            using (BitmapShader shader = new BitmapShader(source, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
            using (Matrix matrix = new Matrix())
            using (var path = new Path())
            {
                if (cropX != 0 || cropY != 0)
                {
                    matrix.SetTranslate(-cropX, -cropY);
                    shader.SetLocalMatrix(matrix);
                }

                paint.SetShader(shader);
                paint.AntiAlias = true;

                // TopLeft
                if (cornersTransformType.HasFlag(CornerTransformType.TopLeftCut))
                {
                    path.MoveTo(0, (float)topLeftCornerSize);
                    path.LineTo((float)topLeftCornerSize, 0);
                }
                else if (cornersTransformType.HasFlag(CornerTransformType.TopLeftRounded))
                {
                    path.MoveTo(0, (float)topLeftCornerSize);
                    path.QuadTo(0, 0, (float)topLeftCornerSize, 0);
                }
                else
                {
                    path.MoveTo(0, 0);
                }

                // TopRight
                if (cornersTransformType.HasFlag(CornerTransformType.TopRightCut))
                {
                    path.LineTo((float)(desiredWidth - topRightCornerSize), 0);
                    path.LineTo((float)desiredWidth, (float)topRightCornerSize);
                }
                else if (cornersTransformType.HasFlag(CornerTransformType.TopRightRounded))
                {
                    path.LineTo((float)(desiredWidth - topRightCornerSize), 0);
                    path.QuadTo((float)desiredWidth, 0, (float)desiredWidth, (float)topRightCornerSize);
                }
                else
                {
                    path.LineTo((float)desiredWidth ,0);
                }

                // BottomRight
                if (cornersTransformType.HasFlag(CornerTransformType.BottomRightCut))
                {
                    path.LineTo((float)desiredWidth, (float)(desiredHeight - bottomRightCornerSize));
                    path.LineTo((float)(desiredWidth - bottomRightCornerSize), (float)desiredHeight);
                }
                else if (cornersTransformType.HasFlag(CornerTransformType.BottomRightRounded))
                {
                    path.LineTo((float)desiredWidth, (float)(desiredHeight - bottomRightCornerSize));
                    path.QuadTo((float)desiredWidth, (float)desiredHeight, (float)(desiredWidth - bottomRightCornerSize), (float)desiredHeight);
                }
                else
                {
                    path.LineTo((float)desiredWidth, (float)desiredHeight);
                }

                // BottomLeft
                if (cornersTransformType.HasFlag(CornerTransformType.BottomLeftCut))
                {
                    path.LineTo((float)bottomLeftCornerSize, (float)desiredHeight);
                    path.LineTo(0, (float)(desiredHeight - bottomLeftCornerSize));
                }
                else if (cornersTransformType.HasFlag(CornerTransformType.BottomLeftRounded))
                {
                    path.LineTo((float)bottomLeftCornerSize, (float)desiredHeight);
                    path.QuadTo(0, (float)desiredHeight, 0, (float)(desiredHeight - bottomLeftCornerSize));
                }
                else
                {
                    path.LineTo(0, (float)desiredHeight);
                }

                path.Close();
                canvas.DrawPath(path, paint);

                return bitmap;
            }
        }
    }
}

