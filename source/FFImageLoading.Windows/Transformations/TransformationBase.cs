using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public abstract class TransformationBase : ITransformation
    {
        public abstract string Key { get; }

        public IBitmap Transform(IBitmap source)
        {
            return Transform(source.ToNative());
        }

        protected abstract BitmapHolder Transform(BitmapHolder source);
    }
}
