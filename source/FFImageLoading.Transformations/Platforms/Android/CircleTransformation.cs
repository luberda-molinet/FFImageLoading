using System;
using FFImageLoading.Work;
using Android.Graphics;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
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

        protected override Bitmap Transform(Bitmap sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
        {
            return RoundedTransformation.ToRounded(sourceBitmap, 0f, 1f, 1f, BorderSize, BorderHexColor);
        }
    }
}

