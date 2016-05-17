//
// SelfDisposingBitmapDrawable.cs
//
// Author:
//   Brett Duncavage <brett.duncavage@rd.io>
//
// Copyright 2013 Rdio, Inc.
//

using Android.Content.Res;
using Android.Graphics;
using System;
using Android.Graphics.Drawables;
using Android.Util;
using FFImageLoading.Collections;
using Android.Runtime;

namespace FFImageLoading.Drawables
{
    /// <summary>
    /// A BitmapDrawable that uses reference counting to determine when internal resources
    /// should be freed (Disposed).
    /// 
    /// On Android versions Honeycomb and higher the internal Bitmap is Dispose()d but not recycled.
    /// On all other Android versions the Bitmap is recycled then disposed.
    /// </summary>
    public class SelfDisposingBitmapDrawable : BitmapDrawable, IByteSizeAware
    {
        private const string TAG = "SelfDisposingBitmapDrawable";

        protected readonly object monitor = new object();

        private int cache_ref_count;
        private int display_ref_count;
        private int retain_ref_count;

        private bool is_bitmap_disposed;

        public string InCacheKey;

        /// <summary>
        /// Occurs when internal displayed reference count has reached 0.
        /// It is raised before the counts are rechecked, and thus before
        /// any resources have potentially been freed.
        /// </summary>
        public event EventHandler NoLongerDisplayed;

        /// <summary>
        /// Occurs when internal displayed reference count goes from 0 to 1.
        /// Once the internal reference count is > 1 this event will not be raised
        /// on subsequent calls to SetIsDisplayed(bool).
        /// </summary>
        public event EventHandler Displayed;

        public long SizeInBytes {
            get {
                if (HasValidBitmap) {
                    return Bitmap.Height * Bitmap.RowBytes;
                }
                return 0;
            }
        }

        public SelfDisposingBitmapDrawable(Resources resources, Bitmap bitmap) : base(resources, bitmap)
        {
			if (bitmap == null)
				throw new ArgumentNullException("Parameter 'bitmap' cannot be null");
        }

		public SelfDisposingBitmapDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        /// <summary>
        /// This should only be called by Views that are actually going to draw the drawable.
        ///
        /// Increments or decrements the internal displayed reference count.
        /// If the internal reference count becomes 0, NoLongerDisplayed will be raised.
        /// If the internal reference count becomes 1, Displayed will be raised.
        /// This method should be called from the main thread.
        /// </summary>
        /// <param name="isDisplayed">If set to <c>true</c> reference count is
        /// incremented, otherwise it is decremented.</param>
        public void SetIsDisplayed(bool isDisplayed)
        {
            EventHandler handler = null;
            lock (monitor) 
			{
				if (isDisplayed && !HasValidBitmap) 
				{
					System.Diagnostics.Debug.WriteLine ("Cannot redisplay this drawable, its resources have been disposed.");
				}
				else if (isDisplayed) 
				{
					display_ref_count++;
					if (display_ref_count == 1) {
						handler = Displayed;
					}
				} 
				else 
				{
					display_ref_count--;
				}

				if (display_ref_count <= 0) {
					handler = NoLongerDisplayed;
				}
            }
            if (handler != null) 
			{
                handler(this, EventArgs.Empty);
            }
            CheckState();
        }

        /// <summary>
        /// This should only be called by caching entities.
        ///
        /// Increments or decrements the cache reference count.
        /// This count represents if the instance is cached by something
        /// and should not free its resources when no longer displayed.
        /// </summary>
        /// <param name="isCached">If set to <c>true</c> is cached.</param>
        public void SetIsCached(bool isCached)
        {
            lock (monitor) {
                if (isCached) {
                    cache_ref_count++;
                } else {
                    cache_ref_count--;
                }
            }
            CheckState();
        }

        /// <summary>
        /// If you wish to use the instance beyond the lifecycle managed by the caching entity
        /// call this method with true. But be aware that you must also have the same number
        /// of calls with false or the instance and its resources will be leaked.
        ///
        /// Also be aware that once retained, the caching entity will not allow the internal
        /// Bitmap allocation to be reused. Retaining an instance does not guarantee it a place
        /// in the cache, it can be evicted at any time.
        /// </summary>
        /// <param name="isRetained">If set to <c>true</c> is retained.</param>
        public void SetIsRetained(bool isRetained)
        {
            lock (monitor) {
                if (isRetained) {
                    retain_ref_count++;
                } else {
                    retain_ref_count--;
                }
            }
            CheckState();
        }

        public bool IsRetained {
            get {
                lock (monitor) {
                    return retain_ref_count > 0;
                }
            }
        }

        protected virtual void OnFreeResources()
        {
            ImageService.Instance.Config.Logger.Debug("SelfDisposingBitmapDrawable.OnFreeResources");
            lock (monitor) {
				if (Bitmap != null && Bitmap.Handle != IntPtr.Zero)
                	Bitmap.Dispose();
				
                is_bitmap_disposed = true;
            }
        }

        private void CheckState()
        {
            lock (monitor) {
                if (ShouldFreeResources) {
                    OnFreeResources();
                }
            }
        }

        private bool ShouldFreeResources {
            get {
                lock (monitor) {
                    return cache_ref_count <= 0 &&
                    display_ref_count <= 0 &&
                    retain_ref_count <= 0 &&
                    HasValidBitmap;
                }
            }
        }

        public virtual bool HasValidBitmap {
            get {
                lock (monitor) {
					return Bitmap != null && Bitmap.Handle != IntPtr.Zero && !is_bitmap_disposed && !Bitmap.IsRecycled;
                }
            }
        }
    }
}

