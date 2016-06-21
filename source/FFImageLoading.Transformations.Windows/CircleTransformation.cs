using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class CircleTransformation : TransformationBase
    {
        public CircleTransformation() : this(0d, null)
        {
        }

		public CircleTransformation(double borderSize, string borderHexColor)
		{
			BorderSize = borderSize;
			BorderHexColor = borderHexColor;
		}

		public double BorderSize { get; set; }
		public string BorderHexColor { get; set; }


		public override string Key
        {
            get { return string.Format("CircleTransformation,borderSize={0},borderHexColor={1}", BorderSize, BorderHexColor); }
        }

        protected override BitmapHolder Transform(BitmapHolder source)
        {
            return RoundedTransformation.ToRounded(source, 0, 1f, 1f, BorderSize, BorderHexColor);
        }
    }
}
