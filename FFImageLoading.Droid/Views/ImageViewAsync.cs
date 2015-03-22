using System;
using Android.Content;
using Android.Util;
using System.Drawing;
using FFImageLoading.Extensions;
using Android.Runtime;

namespace FFImageLoading.Views
{
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

		private BottomAlign _bottomAlign;
		/// <summary>
		/// Gets or sets a value if the image should be aligned to left and bottom in the available space.
		/// <remarks>AdjustViewToBounds should be false and ScaleType should be matrix.</remarks>
		/// </summary>
		public BottomAlign BottomAlign
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

		protected override void OnDetachedFromWindow()
		{
			CancelLoading();
			base.OnDetachedFromWindow();
		}

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
				var matrix = this.ImageMatrix;
				float scaleFactor = 1f;
				float scaleFactorWidth, scaleFactorHeight;
				scaleFactorWidth = (float)Width / (float)Drawable.IntrinsicWidth;
				scaleFactorHeight = (float)Height / (float)Drawable.IntrinsicHeight;

				if (ScaleToFit)
				{
					if (scaleFactorHeight < scaleFactorWidth)
					{
						scaleFactor = scaleFactorHeight;
					}
					else
					{
						scaleFactor = scaleFactorWidth;
					}
				}
				matrix.SetScale(scaleFactor, scaleFactor, 0, 0);

				if (BottomAlign != BottomAlign.None)
				{
					//align to the bottom
					if (Height - (Drawable.IntrinsicHeight * scaleFactor) > 0)
					{
						matrix.PostTranslate(0, Height - (Drawable.IntrinsicHeight * scaleFactor));
					}

					if (Width - (Drawable.IntrinsicWidth * scaleFactor) > 0)
					{
						switch (BottomAlign)
						{
							case BottomAlign.Left:
								//by default is aligned to the left
								break;
							case BottomAlign.Center:
								matrix.PostTranslate((Width - (Drawable.IntrinsicWidth * scaleFactor)) / 2, 0);
								break;
							case BottomAlign.Right:
								matrix.PostTranslate(Width - (Drawable.IntrinsicWidth * scaleFactor), 0);
								break;
						}
					}
				}

				ImageMatrix = matrix;
			}

			return base.SetFrame(l, t, r, b);
		}
	}

	public enum BottomAlign
	{
		None,
		Left,
		Center,
		Right
	}
}