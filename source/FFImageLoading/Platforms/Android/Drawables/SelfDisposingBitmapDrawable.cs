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
using Android.Runtime;
using System.IO;

namespace FFImageLoading.Drawables
{
    /// <summary>
    /// A BitmapDrawable that uses reference counting to determine when internal resources
    /// should be freed (Disposed).
    ///
    /// On Android versions Honeycomb and higher the internal Bitmap is Dispose()d but not recycled.
    /// On all other Android versions the Bitmap is recycled then disposed.
    /// </summary>
    public class SelfDisposingBitmapDrawable : BitmapDrawable, ISelfDisposingBitmapDrawable
    {
        protected readonly object _monitor = new object();
        private int _cacheRefCount;
        private int _displayRefCount;
        private int _retainRefCount;
        private bool _isBitmapDisposed;

        [Obsolete]
        public SelfDisposingBitmapDrawable() : base()
        {
            UpdateSizeInBytes();
        }

        [Obsolete]
        public SelfDisposingBitmapDrawable(Resources resources) : base(resources)
        {
            UpdateSizeInBytes();
        }

        public SelfDisposingBitmapDrawable(Resources resources, Stream stream) : base(resources, stream)
        {
            UpdateSizeInBytes();
        }

        public SelfDisposingBitmapDrawable(Resources resources, string filePath) : base(resources, filePath)
        {
            UpdateSizeInBytes();
        }

        [Obsolete]
        public SelfDisposingBitmapDrawable(Bitmap bitmap) : base(bitmap)
        {
            UpdateSizeInBytes();
        }

        public SelfDisposingBitmapDrawable(Resources resources, Bitmap bitmap) : base(resources, bitmap)
        {
            UpdateSizeInBytes();
        }

        public SelfDisposingBitmapDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            UpdateSizeInBytes();
        }

        public string InCacheKey { get; set; }

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

        private void UpdateSizeInBytes()
        {
            SizeInBytes = HasValidBitmap ? Bitmap.Height * Bitmap.RowBytes : 0;
        }

        public int SizeInBytes { get; private set; }

        public void SetNoLongerDisplayed()
        {
            lock (_monitor)
            {
                if (_displayRefCount <= 0)
                    return;

                _displayRefCount = 1;
                _retainRefCount = 0;
                _cacheRefCount = 0;
                SetIsDisplayed(false);
            }
        }

        /// <summary>
        /// This should only be called by Views that are actually going to draw the drawable.
        /// Increments or decrements the internal displayed reference count.
        /// If the internal reference count becomes 0, NoLongerDisplayed will be raised.
        /// If the internal reference count becomes 1, Displayed will be raised.
        /// This method should be called from the main thread.
        /// </summary>
        /// <param name="isDisplayed">If set to <c>true</c> reference count is
        /// incremented, otherwise it is decremented.</param>
        public virtual void SetIsDisplayed(bool isDisplayed)
        {
            EventHandler handler = null;

            lock (_monitor)
            {
                if (!isDisplayed && _displayRefCount <= 0)
                    return;

                if (isDisplayed && !HasValidBitmap)
                {
                    ImageService.Instance.Config.Logger.Error("Cannot display drawable, its resources have been disposed.");
                }
                else if (isDisplayed)
                {
                    _displayRefCount++;
                    if (_displayRefCount == 1)
                    {
                        handler = Displayed;
                    }
                }
                else
                {
                    _displayRefCount--;

                    if (_displayRefCount <= 0)
                    {
                        handler = NoLongerDisplayed;
                    }
                }
            }

            handler?.Invoke(this, EventArgs.Empty);
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
            lock (_monitor)
            {
                if (isCached)
                {
                    _cacheRefCount++;
                }
                else {
                    _cacheRefCount--;
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
            lock (_monitor)
            {
                if (isRetained)
                {
                    _retainRefCount++;
                }
                else {
                    _retainRefCount--;
                }

                CheckState();
            }
        }

        public bool IsRetained
        {
            get
            {
                lock (_monitor)
                {
                    return _retainRefCount > 0;
                }
            }
        }

        protected virtual void OnFreeResources()
        {
            lock (_monitor)
            {
                _isBitmapDisposed = true;

                // TODO
                // removed for scenarios when Bitmap from Drawable is used somewhere else independently
                // if (Bitmap != null && Bitmap.Handle != IntPtr.Zero)
                // Bitmap.TryDispose();
            }
        }

        private void CheckState()
        {
            lock (_monitor)
            {
                if (_cacheRefCount <= 0 &&
                    _displayRefCount <= 0 &&
                    _retainRefCount <= 0 &&
                    HasValidBitmap)
                {
                    OnFreeResources();
                }
            }
        }

        public virtual bool HasValidBitmap
        {
            get
            {
                lock (_monitor)
                {
                    if (_isBitmapDisposed)
                        return false;

                    try
                    {
                        return Bitmap != null && Bitmap.Handle != IntPtr.Zero && !Bitmap.IsRecycled;
                    }
                    catch (ObjectDisposedException)
                    {
                        return false;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            SetNoLongerDisplayed();
        }

        protected override void JavaFinalize()
        {
            base.JavaFinalize();
            SetNoLongerDisplayed();
        }

        // TODO Measure if it's necessary
        //~SelfDisposingBitmapDrawable()
        //{
        //	SetNoLongerDisplayed();
        //}
    }
}

