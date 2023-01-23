using System;
using Android.Graphics;

namespace FFImageLoading.Drawables
{
    public interface ISelfDisposingBitmapDrawable : IByteSizeAware
    {
        string InCacheKey { get; set; }

        /// <summary>
        /// Occurs when internal displayed reference count has reached 0.
        /// It is raised before the counts are rechecked, and thus before
        /// any resources have potentially been freed.
        /// </summary>
        event EventHandler NoLongerDisplayed;

        /// <summary>
        /// Occurs when internal displayed reference count goes from 0 to 1.
        /// Once the internal reference count is > 1 this event will not be raised
        /// on subsequent calls to SetIsDisplayed(bool).
        /// </summary>
        event EventHandler Displayed;

        void SetNoLongerDisplayed();

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
        void SetIsDisplayed(bool isDisplayed);

        /// <summary>
        /// This should only be called by caching entities.
        ///
        /// Increments or decrements the cache reference count.
        /// This count represents if the instance is cached by something
        /// and should not free its resources when no longer displayed.
        /// </summary>
        /// <param name="isCached">If set to <c>true</c> is cached.</param>
        void SetIsCached(bool isCached);

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
        void SetIsRetained(bool isRetained);

        bool IsRetained { get; }

        bool HasValidBitmap { get; }

        IntPtr Handle { get; }

        Bitmap Bitmap { get; }
    }
}
