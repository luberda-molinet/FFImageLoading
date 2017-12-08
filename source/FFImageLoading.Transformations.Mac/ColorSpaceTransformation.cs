using System;
using AppKit;
using CoreGraphics;
using CoreImage;
using System.Linq;
using Foundation;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class ColorSpaceTransformation: TransformationBase
    {
        CGColorSpace _colorSpace;
        CIColorMatrix _colorMatrix;
        float[][] _rgbawMatrix;

        public ColorSpaceTransformation() : this(FFColorMatrix.InvertColorMatrix)
        {
        }

        public ColorSpaceTransformation(float[][] rgbawMatrix)
        {
            if (rgbawMatrix.Length != 5 || rgbawMatrix.Any(v => v.Length != 5))
                throw new ArgumentException("Wrong size of RGBAW color matrix");

            _colorSpace = null;
            _colorMatrix = new CIColorMatrix();
            RGBAWMatrix = rgbawMatrix;
        }

        public ColorSpaceTransformation(CGColorSpace colorSpace)
        {
            _colorSpace = colorSpace;
            _colorMatrix = null;
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

                _colorSpace = null;
                _rgbawMatrix = value;
                UpdateColorMatrix(_rgbawMatrix);
            }
        }

        public override string Key
        {
            get
            {
                if (_rgbawMatrix == null)
                    return string.Format("ColorSpaceTransformation,colorSpace={0}", _colorSpace.GetHashCode());

                return string.Format("ColorSpaceTransformation,rgbawMatrix={0}",
                    string.Join(",", _rgbawMatrix.Select(x => string.Join(",", x)).ToArray()));
            }
        }

        void UpdateColorMatrix(float[][] ma)
        {
            _colorMatrix.RVector = new CIVector(ma[0][0], ma[1][0], ma[2][0], ma[3][0]);
            _colorMatrix.GVector = new CIVector(ma[0][1], ma[1][1], ma[2][1], ma[3][1]);
            _colorMatrix.BVector = new CIVector(ma[0][2], ma[1][2], ma[2][2], ma[3][2]);
            _colorMatrix.AVector = new CIVector(ma[0][3], ma[1][3], ma[2][3], ma[3][3]);
            _colorMatrix.BiasVector = new CIVector(ma[0][4], ma[1][4], ma[2][4], ma[3][4]);
        }

        protected override NSImage Transform(NSImage sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            if (_colorMatrix != null)
            {
                return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() => ToFilter(sourceBitmap, _colorMatrix));
            }
            else
            {
                return Helpers.MainThreadDispatcher.PostForResult<NSImage>(() => ToColorSpace(sourceBitmap, _colorSpace));
            }
        }

        public static NSImage ToColorSpace(NSImage source, CGColorSpace colorSpace)
        {
            CGRect bounds = new CGRect(0, 0, source.CGImage.Width, source.CGImage.Height);
            var cgImage = source.CGImage;

            using (var context = new CGBitmapContext(IntPtr.Zero, (int)bounds.Width, (int)bounds.Height, cgImage.BitsPerComponent, cgImage.BytesPerRow, colorSpace, CGImageAlphaInfo.None))
            {
                context.DrawImage(bounds, source.CGImage);
                using (var imageRef = context.ToImage())
                {
                    return new NSImage(imageRef, CGSize.Empty);
                }
            }
        }

        public static NSImage ToFilter(NSImage source, CIFilter filter)
        {
            using (var inputImage = CIImage.FromCGImage(source.CGImage))
            {
                filter.Image = inputImage;
                using (var resultImage = new NSCIImageRep(filter.OutputImage))
                {
                    var nsImage = new NSImage(resultImage.Size);
                    nsImage.AddRepresentation(resultImage);
                    return nsImage;
                }
            }
        }
    }
}

