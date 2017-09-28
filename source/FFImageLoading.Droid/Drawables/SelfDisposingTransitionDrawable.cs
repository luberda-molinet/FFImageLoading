//using System;
//using Android.Graphics;
//using Android.Graphics.Drawables;
//using FFImageLoading.Drawables;

//namespace FFImageLoading
//{
//    public class SelfDisposingTransitionDrawable : TransitionDrawable, ISelfDisposingBitmapDrawable
//    {
//        const string TAG = "SelfDisposingTransitionDrawable";
//        protected readonly object monitor = new object();

//        int cache_ref_count;
//        int display_ref_count;
//        int retain_ref_count;
//        bool is_bitmap_disposed;

//        public SelfDisposingTransitionDrawable(SelfDisposingBitmapDrawable image, SelfDisposingBitmapDrawable placeholder) : base(new[] { placeholder.Mutate(), image })
//        {
//            CrossFadeEnabled = false;
//            PlaceholderDrawable = GetDrawable(0) as SelfDisposingBitmapDrawable;
//            ImageDrawable = image;
//        }

//        protected SelfDisposingBitmapDrawable ImageDrawable { get; private set; }
//        protected SelfDisposingBitmapDrawable PlaceholderDrawable { get; private set; }

//        public Bitmap Bitmap
//        {
//            get
//            {
//                return ImageDrawable?.Bitmap;
//            }
//        }

//        public string InCacheKey { get; set; }

//        /// <summary>
//        /// Occurs when internal displayed reference count has reached 0.
//        /// It is raised before the counts are rechecked, and thus before
//        /// any resources have potentially been freed.
//        /// </summary>
//        public event EventHandler NoLongerDisplayed;

//        /// <summary>
//        /// Occurs when internal displayed reference count goes from 0 to 1.
//        /// Once the internal reference count is > 1 this event will not be raised
//        /// on subsequent calls to SetIsDisplayed(bool).
//        /// </summary>
//        public event EventHandler Displayed;

//        public long SizeInBytes
//        {
//            get
//            {
//                if (HasValidBitmap)
//                {
//                    return Bitmap.Height * Bitmap.RowBytes;
//                }
//                return 0;
//            }
//        }

//        public void SetNoLongerDisplayed()
//        {
//            lock (monitor)
//            {
//                display_ref_count = 0;
//                SetIsDisplayed(false);
//            }
//        }

//        public override void Draw(Canvas canvas)
//        {
//            try
//            {
//                base.Draw(canvas);
//            }
//            catch (Exception) { }
//        }

//        /// <summary>
//        /// This should only be called by Views that are actually going to draw the drawable.
//        /// Increments or decrements the internal displayed reference count.
//        /// If the internal reference count becomes 0, NoLongerDisplayed will be raised.
//        /// If the internal reference count becomes 1, Displayed will be raised.
//        /// This method should be called from the main thread.
//        /// </summary>
//        /// <param name="isDisplayed">If set to <c>true</c> reference count is
//        /// incremented, otherwise it is decremented.</param>
//        public void SetIsDisplayed(bool isDisplayed)
//        {
//            ImageDrawable?.SetIsDisplayed(isDisplayed);
//            PlaceholderDrawable?.SetIsDisplayed(isDisplayed);

//            EventHandler handler = null;
//            lock (monitor)
//            {
//                if (isDisplayed && !HasValidBitmap)
//                {
//                    System.Diagnostics.Debug.WriteLine("Cannot redisplay this drawable, its resources have been disposed.");
//                }
//                else if (isDisplayed)
//                {
//                    display_ref_count++;
//                    if (display_ref_count == 1)
//                    {
//                        handler = Displayed;
//                    }
//                }
//                else
//                {
//                    display_ref_count--;
//                }

//                if (display_ref_count <= 0)
//                {
//                    display_ref_count = 0;
//                    handler = NoLongerDisplayed;
//                }
//            }
//            if (handler != null)
//            {
//                handler(this, EventArgs.Empty);
//            }
//            CheckState();
//        }

//        /// <summary>
//        /// This should only be called by caching entities.
//        ///
//        /// Increments or decrements the cache reference count.
//        /// This count represents if the instance is cached by something
//        /// and should not free its resources when no longer displayed.
//        /// </summary>
//        /// <param name="isCached">If set to <c>true</c> is cached.</param>
//        public void SetIsCached(bool isCached)
//        {
//            ImageDrawable?.SetIsCached(isCached);
//            PlaceholderDrawable?.SetIsCached(isCached);

//            lock (monitor)
//            {
//                if (isCached)
//                {
//                    cache_ref_count++;
//                }
//                else
//                {
//                    cache_ref_count--;
//                }
//            }
//            CheckState();
//        }

//        /// <summary>
//        /// If you wish to use the instance beyond the lifecycle managed by the caching entity
//        /// call this method with true. But be aware that you must also have the same number
//        /// of calls with false or the instance and its resources will be leaked.
//        ///
//        /// Also be aware that once retained, the caching entity will not allow the internal
//        /// Bitmap allocation to be reused. Retaining an instance does not guarantee it a place
//        /// in the cache, it can be evicted at any time.
//        /// </summary>
//        /// <param name="isRetained">If set to <c>true</c> is retained.</param>
//        public void SetIsRetained(bool isRetained)
//        {
//            ImageDrawable?.SetIsRetained(isRetained);
//            PlaceholderDrawable?.SetIsRetained(isRetained);

//            lock (monitor)
//            {
//                if (isRetained)
//                {
//                    retain_ref_count++;
//                }
//                else {
//                    retain_ref_count--;
//                }
//            }
//            CheckState();
//        }

//        public bool IsRetained
//        {
//            get
//            {
//                lock (monitor)
//                {
//                    return retain_ref_count > 0;
//                }
//            }
//        }

//        protected virtual void OnFreeResources()
//        {
//            lock (monitor)
//            {
//                if (Bitmap != null && Bitmap.Handle != IntPtr.Zero)
//                    Bitmap.TryDispose();

//                is_bitmap_disposed = true;
//            }
//        }

//        void CheckState()
//        {
//            lock (monitor)
//            {
//                if (ShouldFreeResources)
//                {
//                    OnFreeResources();
//                }
//            }
//        }

//        bool ShouldFreeResources
//        {
//            get
//            {
//                lock (monitor)
//                {
//                    return cache_ref_count <= 0 &&
//                    display_ref_count <= 0 &&
//                    retain_ref_count <= 0 &&
//                    HasValidBitmap;
//                }
//            }
//        }

//        public virtual bool HasValidBitmap
//        {
//            get
//            {
//                lock (monitor)
//                {
//                    var bitmap = Bitmap;
//                    return bitmap != null && bitmap.Handle != IntPtr.Zero && !is_bitmap_disposed && !bitmap.IsRecycled;
//                }
//            }
//        }
//    }
//}
