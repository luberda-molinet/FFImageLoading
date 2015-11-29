using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class CircleTransformation : TransformationBase
    {
        public CircleTransformation()
        {
        }

        public override string Key
        {
            get { return "CircleTransformation"; }
        }

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            RoundedTransformation.ToRounded(source, 0, 1f, 1f);
            return source;
        }
    }
}
