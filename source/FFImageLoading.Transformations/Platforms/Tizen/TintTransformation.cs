using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class TintTransformation : ITransformation
    {
        public TintTransformation() : this(0, 165, 0, 128)
        {
        }

        public TintTransformation(int r, int g, int b, int a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public TintTransformation(string hexColor)
        {
            HexColor = hexColor;
        }


        public bool EnableSolidColor { get; set; }

        public string HexColor { get; set; }

        public int R { get; set; }

        public int G { get; set; }

        public int B { get; set; }

        public int A { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public string Key => $"TintTransformation,R={R},G={G},B={B},A={A},HexColor={HexColor},EnableSolidColor={EnableSolidColor}";
    }
}