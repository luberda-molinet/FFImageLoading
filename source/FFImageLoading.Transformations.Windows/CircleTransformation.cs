using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class CircleTransformation : TransformationBase
    {
        private double _borderSize;
        private string _borderHexColor;

        public CircleTransformation() : this(0d, null)
        {
        }

        public CircleTransformation(double borderSize, string borderHexColor)
        {
            _borderSize = borderSize;
            _borderHexColor = borderHexColor;
        }

        public override string Key
        {
            get { return string.Format("CircleTransformation,borderSize={0},borderHexColor={1}", _borderSize, _borderHexColor); }
        }

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            RoundedTransformation.ToRounded(source, 0, 1f, 1f, _borderSize, _borderHexColor);
            return source;
        }
    }
}
