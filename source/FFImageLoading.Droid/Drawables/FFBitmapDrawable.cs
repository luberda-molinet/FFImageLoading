using System;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.OS;
using Android.Content.Res;
using Android.Graphics;

namespace FFImageLoading.Drawables
{
	public class FFBitmapDrawable : SelfDisposingBitmapDrawable
	{
		private readonly float _fadingTime;
		private readonly long _startTimeMillis;
		private int _alpha = 0xFF;
		private Drawable _placeholder;
		private volatile bool _animating;

		public FFBitmapDrawable(Resources res, Bitmap bitmap, Drawable placeholder, float fadingTime, bool fadeEnabled)
			: base(res, bitmap)
		{
			_placeholder = placeholder;
			_fadingTime = fadingTime;
			_animating = fadeEnabled;
			_startTimeMillis = SystemClock.UptimeMillis();
		}

		public FFBitmapDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

		public override void Draw(Canvas canvas)
		{
			try {
				if (!_animating)
				{
					base.SetAlpha(_alpha);
					base.Draw(canvas);
				}
				else
				{
					var uptime = SystemClock.UptimeMillis();
					float normalized = (uptime - _startTimeMillis) / _fadingTime;
					if (normalized >= 1f)
					{
						_animating = false;
						_placeholder = null;
						base.Draw(canvas);
					}
					else
					{
						if (_placeholder != null)
						{
							_placeholder.Draw(canvas);
						}

						int partialAlpha = (int)(_alpha * normalized);
						base.SetAlpha(partialAlpha);
						base.Draw(canvas);
						base.SetAlpha(_alpha);
					}
				}
			} catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}


		public void StopFadeAnimation()
		{
			_animating = false;
			_placeholder = null;
		}

		public override void SetAlpha(int alpha)
		{
			_alpha = alpha;

			if (_placeholder != null)
			{
				_placeholder.SetAlpha(alpha);
			}
			base.SetAlpha(alpha);
		}

		public override void SetColorFilter(Color color, PorterDuff.Mode mode)
		{
			if (_placeholder != null)
			{
				_placeholder.SetColorFilter(color, mode);
			}
			base.SetColorFilter(color, mode);
		}

		protected override void OnBoundsChange(Rect bounds)
		{
			if (_placeholder != null)
			{
				_placeholder.SetBounds(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
			}
			base.OnBoundsChange(bounds);
		}
	}
}

