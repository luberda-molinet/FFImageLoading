using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class FlipTransformation : TransformationBase
    {
        public FlipTransformation() : this(FlipType.Horizontal)
        {
        }

        public FlipTransformation(FlipType flipType)
        {
            FlipType = flipType;
        }

        public override string Key
        {
            get { return string.Format("FlipTransformation,Type={0}", FlipType); }
        }

        public FlipType FlipType { get; set; }

        protected override BitmapHolder Transform(BitmapHolder bitmapSource, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return ToFlipped(bitmapSource, FlipType);
        }

        public static BitmapHolder ToFlipped(BitmapHolder bmp, FlipType flipMode)
        {
            // Use refs for faster access (really important!) speeds up a lot!
            var w = bmp.Width;
            var h = bmp.Height;
            var i = 0;
            BitmapHolder result = new BitmapHolder(new byte[bmp.PixelData.Length], w, h);

            if (flipMode == FlipType.Vertical)
            {
                var rp = result.PixelData;
                for (var y = h - 1; y >= 0; y--)
                {
                    for (var x = 0; x < w; x++)
                    {
                        var srcInd = y * w + x;
                        result.SetPixel(i, bmp.GetPixel(srcInd));
                        i++;
                    }
                }
            }
            else
            {
                var rp = result.PixelData;
                for (var y = 0; y < h; y++)
                {
                    for (var x = w - 1; x >= 0; x--)
                    {
                        var srcInd = y * w + x;
                        result.SetPixel(i, bmp.GetPixel(srcInd));
                        i++;
                    }
                }
            }

            return result;
        }
    }
}
