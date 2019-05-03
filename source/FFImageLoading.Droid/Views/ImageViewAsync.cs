using System;
using Android.Content;
using Android.Util;
using Android.Runtime;
using Android.Widget;

namespace FFImageLoading.Views
{
    [Preserve(AllMembers = true)]
    [Register("ffimageloading.views.ImageViewAsync")]
	[Obsolete("You can now use Android's ImageView")]
    public class ImageViewAsync : ImageView
    {
        private bool _customFit;

        public ImageViewAsync(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public ImageViewAsync(Context context) : base(context)
        {
        }

        public ImageViewAsync(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public ImageViewAsync(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
        }

		public void CancelLoading()
		{
			ImageService.Instance.CancelWorkForView(this);
		}

		private bool _scaleToFit;
        /// <summary>
        /// Gets or sets a value if the image should be scale to fit in the available space keeping aspect ratio.
        /// <remarks>AdjustViewToBounds should be false and ScaleType should be matrix.</remarks>
        /// </summary>
        public bool ScaleToFit
        {
            get => _scaleToFit;
            set
            {
                _customFit = true;
                _scaleToFit = value;
                SetAdjustViewBounds(false);
                SetScaleType(ScaleType.Matrix);
            }
        }

        private AlignMode _bottomAlign;
        /// <summary>
        /// Gets or sets a value if the image should be aligned to left and bottom in the available space.
        /// <remarks>AdjustViewToBounds should be false and ScaleType should be matrix.</remarks>
        /// </summary>
        public AlignMode AlignMode
        {
            get => _bottomAlign;
            set
            {
                _customFit = true;
                _bottomAlign = value;
                SetAdjustViewBounds(false);
                SetScaleType(ScaleType.Matrix);
            }
        }

        /* FMT: this is not fine when working with RecyclerView... It can detach and cache the view, then reattach it
        protected override void OnDetachedFromWindow()
        {
            CancelLoading();
            base.OnDetachedFromWindow();
        }*/

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            if (_customFit && Drawable == null)
            {
                SetMeasuredDimension(widthMeasureSpec, heightMeasureSpec);
            }
            else
            {
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            }
        }

        protected override bool SetFrame(int l, int t, int r, int b)
        {
            if (_customFit && Drawable != null && Drawable.IntrinsicWidth != 0)
            {
                var bottomAlignmentDefined = AlignMode != AlignMode.None;
                if (ScaleToFit || bottomAlignmentDefined)
                {
                    var matrix = ImageMatrix;
                    var scaleFactor = 1f;

                    if (ScaleToFit)
                    {
                        var scaleFactorWidth = (float)Width / Drawable.IntrinsicWidth;
                        var scaleFactorHeight = (float)Height / Drawable.IntrinsicHeight;

                        if (scaleFactorHeight < scaleFactorWidth)
                        {
                            scaleFactor = scaleFactorHeight;
                        }
                        else
                        {
                            scaleFactor = scaleFactorWidth;
                        }

                        if (Math.Abs(scaleFactor - 1f) > float.Epsilon)
                        {
                            matrix.SetScale(scaleFactor, scaleFactor, 0, 0);
                        }
                    }

                    if (AlignMode != AlignMode.None)
                    {
                        if (AlignMode != AlignMode.TopCenter && Height - (Drawable.IntrinsicHeight * scaleFactor) > 0)
                        {
                            //align to the bottom
                            matrix.PostTranslate(0, Height - (Drawable.IntrinsicHeight * scaleFactor));
                        }

                        if (Width - (Drawable.IntrinsicWidth * scaleFactor) > 0)
                        {
                            switch (AlignMode)
                            {
                                case AlignMode.BottomLeft:
                                    //by default is aligned to the left
                                    break;
                                case AlignMode.BottomCenter:
                                    matrix.PostTranslate((Width - (Drawable.IntrinsicWidth * scaleFactor)) / 2, 0);
                                    break;
                                case AlignMode.BottomRight:
                                    matrix.PostTranslate(Width - (Drawable.IntrinsicWidth * scaleFactor), 0);
                                    break;
                                case AlignMode.TopCenter:
                                    matrix.PostTranslate((Width - (Drawable.IntrinsicWidth * scaleFactor)) / 2, 0);
                                    break;
                            }
                        }


                    }

                    ImageMatrix = matrix;
                }
            }

            return base.SetFrame(l, t, r, b);
        }
    }

    public enum AlignMode
    {
        None,
        TopCenter,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
}
