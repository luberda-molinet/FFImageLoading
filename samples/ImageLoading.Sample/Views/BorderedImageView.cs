using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using FFImageLoading.Views;

namespace ImageLoading.Sample.Views
{
    public class BorderedImageView : ImageView
    {
        private const int STROKE_WIDTH_DP = 4;

        private Paint mBorderPaint;

        public BorderedImageView(Context context)
            : base(context)
        {
            Init(context, null);
        }

        public BorderedImageView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Init(context, attrs);
        }

        public BorderedImageView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        private void Init(Context context, IAttributeSet attrs)
        {
            Android.Content.Res.TypedArray attributes = context.ObtainStyledAttributes(attrs, Resource.Styleable.BorderedImageView, 0, 0);
            var color = attributes.GetColor(Resource.Styleable.BorderedImageView_bordered_color, Color.White);

            mBorderPaint = new Paint();
            mBorderPaint.AntiAlias = true;
            mBorderPaint.SetStyle(Paint.Style.Stroke);
            mBorderPaint.Color = color;
            mBorderPaint.StrokeWidth = ImageLoading.Sample.MainActivity.DimensionHelper.DpToPx(STROKE_WIDTH_DP);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            canvas.DrawRect(0, 0, Width, Height, mBorderPaint);
        }
    }
}
