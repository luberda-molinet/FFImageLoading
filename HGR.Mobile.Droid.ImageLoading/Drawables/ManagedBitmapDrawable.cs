using System;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using HGR.Mobile.Droid.ImageLoading.Helpers;

namespace HGR.Mobile.Droid.ImageLoading.Drawables
{
	public class ManagedBitmapDrawable : BitmapDrawable
	{
		private readonly object _lck;
		private int _cacheRefCount;
		private int _displayRefCount;
		private bool _disposed;
		private bool _hasBeenDisplayed;

		public ManagedBitmapDrawable(Resources res, Bitmap bitmap) : base(res, bitmap)
		{
			_lck = new object();
		}

		public event EventHandler OnDisplayed;
		public event EventHandler OnNoLongerDisplayed;

		public void SetIsCached(bool isCached)
		{
			lock (_lck)
			{
				if (isCached)

					_cacheRefCount++;
				else

					_cacheRefCount--;

				CheckRefCounts();
			}
		}

		public void SetIsDisplayed(bool isDisplayed)
		{
			EventHandler handler = null;
			lock (_lck)
			{
				if (isDisplayed)
				{
					_displayRefCount++;
					_hasBeenDisplayed = true;
					if (_displayRefCount == 1)
					{
						handler = OnDisplayed;
					}
				}
				else
				{
					_displayRefCount--;
				}

				if (_displayRefCount <= 0)
				{
					handler = OnNoLongerDisplayed;
				}

				CheckRefCounts();
			}

			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}

		private void CheckRefCounts()
		{
			lock (_lck)
			{
				if (_displayRefCount <= 0 && _cacheRefCount <= 0 && _hasBeenDisplayed && HasValidBitmap())
				{

					MiniLogger.Debug("Image no longer being used or cached so recycling...");
					OnFreeResources();
				}
			}
		}

		private bool HasValidBitmap()
		{
			return Bitmap != null && !_disposed && !Bitmap.IsRecycled;
		}

		private void OnFreeResources()
		{
			lock (_lck)
			{
				// if we can't reuse bitmaps, just recycle it
				if (!Utils.HasHoneycomb())
				{
					Bitmap.Recycle();
				}
				Bitmap.Dispose();
				_disposed = true;
			}
		}
	}
}