using System.Linq;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class ColorSpaceTransformation: ITransformation
    {
        public ColorSpaceTransformation()
        {
        }

        public ColorSpaceTransformation(float[][] rgbawMatrix)
        {
            RGBAWMatrix = rgbawMatrix;
        }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public float[][] RGBAWMatrix { get; set; }

        public string Key => $"ColorSpaceTransformation,rgbawMatrix={string.Join(",", RGBAWMatrix.Select(x => string.Join(",", x)).ToArray())}";
    }
}