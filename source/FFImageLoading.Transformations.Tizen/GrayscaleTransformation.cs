using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class GrayscaleTransformation: ITransformation
    {
        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }

        public string Key => "GrayscaleTransformation";
    }
}

