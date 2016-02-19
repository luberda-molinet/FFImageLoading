using System;
using Android.Content;
using Android.Util;
using System.Drawing;
using FFImageLoading.Extensions;
using Android.Runtime;

namespace FFImageLoading.Views
{
	[Register("ffimageloading.views.ImageViewAsync")]
	public class ImageViewAsync : ManagedImageView
	{
		protected SizeF? _predefinedSize;

		public ImageViewAsync(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
			SetWillNotDraw(false);
		}

		public ImageViewAsync(Context context, SizeF? predefinedSize = null)
			: base(context)
		{
			SetWillNotDraw(false);
		}

		public ImageViewAsync(Context context, IAttributeSet attrs, SizeF? predefinedSize)
			: base(context, attrs)
		{
			SetWillNotDraw(false);
		}

		public ImageViewAsync(Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			SetWillNotDraw(false);
		}

		private bool _scaleToFit;
		/// <summary>
		/// Gets or sets a value if the image should be scale to fit in the available space keeping aspect ratio.
		/// <remarks>AdjustViewToBounds should be false and ScaleType should be matrix.</remarks>
		/// </summary>
		public bool ScaleToFit
		{
			get
			{
				return _scaleToFit;
			}
			set
			{
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
			get
			{
				return _bottomAlign;
			}
			set
			{
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

		public void CancelLoading()
		{
			ImageService.CancelWorkFor(this.GetImageLoaderTask());
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (Drawable == null)
			{
				if (_predefinedSize.HasValue)
					SetMeasuredDimension((int)_predefinedSize.Value.Width, (int)_predefinedSize.Value.Height);
				else
					SetMeasuredDimension(widthMeasureSpec, heightMeasureSpec);
			}
			else
			{
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			}
		}

		protected override bool SetFrame(int l, int t, int r, int b)
		{
			if (Drawable != null && Drawable.IntrinsicWidth != 0)
			{
				bool bottomAlignmentDefined = AlignMode != AlignMode.None;
				if (ScaleToFit || bottomAlignmentDefined)
				{
					var matrix = this.ImageMatrix;
					float scaleFactor = 1f;

					if (ScaleToFit)
					{
						float scaleFactorWidth = (float)Width / (float)Drawable.IntrinsicWidth;
						float scaleFactorHeight = (float)Height / (float)Drawable.IntrinsicHeight;

						if (scaleFactorHeight < scaleFactorWidth)
						{
							scaleFactor = scaleFactorHeight;
						}
						else
						{
							scaleFactor = scaleFactorWidth;
						}

						if (scaleFactor != 1f)
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