using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Renderscripts;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Work;

namespace ImageLoading.Sample.Transformations
{
    /// <summary>
    /// https://github.com/wasabeef/picasso-transformations
    /// </summary>
	public class BlurTransformation : TransformationBase
    {
        private const int MAX_RADIUS = 25;

        private Context mContext;

        private int mRadius;

        public BlurTransformation(Context context) : this(context, MAX_RADIUS)
        {

        }

        public BlurTransformation(Context context, int radius)
        {
            mContext = context;
            mRadius = radius;
        }

        public override string Key
        {
            get { return "BlurTransformation(radius=" + mRadius + ")"; }
        }

        protected override Android.Graphics.Bitmap Transform(Android.Graphics.Bitmap source)
        {
            Bitmap outBitmap = Bitmap.CreateBitmap(source.Width, source.Height, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas(outBitmap);
            canvas.DrawBitmap(source, 0, 0, null);

            RenderScript rs = RenderScript.Create(mContext);
            Allocation overlayAlloc = Allocation.CreateFromBitmap(rs, outBitmap);
            ScriptIntrinsicBlur blur = ScriptIntrinsicBlur.Create(rs, overlayAlloc.Element);
            blur.SetInput(overlayAlloc);
            blur.SetRadius(mRadius);
            blur.ForEach(overlayAlloc);
            overlayAlloc.CopyTo(outBitmap);

            source.Recycle();
            rs.Destroy();

            return outBitmap;
        }
    }
}