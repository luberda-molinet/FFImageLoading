using FFImageLoading.Transformations.WritableBitmapEx;
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
            var transformed = ToFlipped(source, _flipType);
            
            source = null; // Free resource as we return a new WriteableBitmap
            return transformed;
        }

        public static WriteableBitmap ToFlipped(WriteableBitmap bmp, FlipType flipMode)
        {
            using (var context = bmp.GetBitmapContext())
            {
                // Use refs for faster access (really important!) speeds up a lot!
                var w = context.Width;
                var h = context.Height;
                var p = context.Pixels;
                var i = 0;
                WriteableBitmap result = null;

                if (flipMode == FlipType.Horizontal)
                {
                    result = BitmapFactory.New(w, h);
                    using (var destContext = result.GetBitmapContext())
                    {
                        var rp = destContext.Pixels;
                        for (var y = h - 1; y >= 0; y--)
                        {
                            for (var x = 0; x < w; x++)
                            {
                                var srcInd = y * w + x;
                                rp[i] = p[srcInd];
                                i++;
                            }
                        }
                    }
                }
                else if (flipMode == FlipType.Vertical)
                {
                    result = BitmapFactory.New(w, h);
                    using (var destContext = result.GetBitmapContext())
                    {
                        var rp = destContext.Pixels;
                        for (var y = 0; y < h; y++)
                        {
                            for (var x = w - 1; x >= 0; x--)
                            {
                                var srcInd = y * w + x;
                                rp[i] = p[srcInd];
                                i++;
                            }
                        }
                    }
                }

                return result;
            }
        }
    }
}
