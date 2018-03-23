using System.Linq;
using System;
using Android.OS;
using Android.Graphics;
using Android.Util;
using FFImageLoading.Helpers;
using FFImageLoading.Drawables;
using Java.Util;

namespace FFImageLoading.Cache
{
    public class ReuseBitmapDrawableCache<TValue> where TValue : Java.Lang.Object, ISelfDisposingBitmapDrawable
    {
        readonly object monitor = new object();
        const string TAG = "ReuseBitmapDrawableCache";

        int total_added;
        int total_removed;
        int total_reuse_hits;
        int total_reuse_misses;
        int total_evictions;
        int total_cache_hits;
        long current_cache_byte_count;

        readonly int high_watermark;
        readonly int low_watermark;
        bool reuse_pool_refill_needed = true;

        /// <summary>
        /// Contains all entries that are currently being displayed. These entries are not eligible for
        /// reuse or eviction. Entries will be added to the reuse pool when they are no longer displayed.
        /// </summary>
        ByteBoundStrongLruCache<TValue> displayed_cache;

        /// <summary>
        /// Contains entries that potentially available for reuse and candidates for eviction.
        /// This is the default location for newly added entries. This cache
        /// is searched along with the displayed cache for cache hits. If a cache hit is found here, its
        /// place in the LRU list will be refreshed. Items only move out of reuse and into displayed
        /// when the entry has SetIsDisplayed(true) called on it.
        /// </summary>
        readonly ByteBoundStrongLruCache<TValue> reuse_pool;
        readonly IMiniLogger log;
        readonly bool _verboseLogging;


        /// <summary>
        /// Initializes a new instance of the <see cref="ReuseBitmapDrawableCache"/> class.
        /// </summary>
        /// <param name="logger">Logger for debug messages</param>
        /// <param name="highWatermark">Maximum number of bytes the reuse pool will hold before starting evictions.</param>
        /// <param name="lowWatermark">Number of bytes the reuse pool will be drained down to after the high watermark is exceeded.</param>
        /// <param name="verboseLogging">If set to <c>true</c> verbose logging.</param>
        public ReuseBitmapDrawableCache(IMiniLogger logger, int memoryCacheSize, int bitmapPoolSize, bool verboseLogging = false)
        {
            _verboseLogging = verboseLogging;
            log = logger;
            low_watermark = bitmapPoolSize / 2;
            high_watermark = bitmapPoolSize;
            displayed_cache = new ByteBoundStrongLruCache<TValue>(memoryCacheSize);
            reuse_pool = new ByteBoundStrongLruCache<TValue>(bitmapPoolSize);
            reuse_pool.EntryRemoved += OnEntryRemovedFromReusePool;
        }


        /// <summary>
        /// Attempts to find a bitmap suitable for reuse based on the given dimensions.
        /// Note that any returned instance will have SetIsRetained(true) called on it
        /// to ensure that it does not release its resources prematurely as it is leaving
        /// cache management. This means you must call SetIsRetained(false) when you no
        /// longer need the instance.
        /// </summary>
        /// <returns>A ISelfDisposingBitmapDrawable that has been retained. You must call SetIsRetained(false)
        /// when finished using it.</returns>
        /// <param name="options">Options.</param>
        public TValue GetReusableBitmapDrawable(BitmapFactory.Options options)
        {
            // Only attempt to get a bitmap for reuse if the reuse cache is full.
            // This prevents us from prematurely depleting the pool and allows
            // more cache hits, as the most recently added entries will have a high
            // likelihood of being accessed again so we don't want to steal those bytes too soon.
            lock (monitor)
            {
                if (reuse_pool.CacheSizeInBytes < low_watermark && reuse_pool_refill_needed)
                {
                    total_reuse_misses++;
                    return null;
                }

                reuse_pool_refill_needed = false;
                TValue reuseDrawable = null;

                var reuse_values = reuse_pool.Values;
                foreach (var bd in reuse_values)
                {
                    if (bd.IsValidAndHasValidBitmap() && bd.Bitmap.IsMutable && !bd.IsRetained && CanUseForInBitmap(bd.Bitmap, options))
                    {
                        reuseDrawable = bd;
                        break;
                    }
                }

                if (reuseDrawable != null)
                {
                    reuseDrawable.SetIsRetained(true);
                    UpdateByteUsage(reuseDrawable.Bitmap, decrement: true, causedByEviction: true);

                    // Cleanup the entry
                    reuseDrawable.Displayed -= OnEntryDisplayed;
                    reuseDrawable.NoLongerDisplayed -= OnEntryNoLongerDisplayed;
                    reuseDrawable.SetIsCached(false);
                    reuse_pool.Remove(reuseDrawable.InCacheKey);
                    total_reuse_hits++;

                    if (_verboseLogging)
                        log?.Debug("[MEMORY_CACHE] Used image from reuse pool for decoding optimization");
                }
                else
                {
                    total_reuse_misses++;
                    // Indicate that the pool may need to be refilled.
                    // There is little harm in setting this flag since it will be unset
                    // on the next reuse request if the threshold is reuse_pool.CacheSizeInBytes >= low_watermark.
                    reuse_pool_refill_needed = true;
                }

                return reuseDrawable;
            }
        }

        bool CanUseForInBitmap(Bitmap candidate, BitmapFactory.Options targetOptions)
        {
            if (Utils.HasKitKat())
            {
                // From Android 4.4 (KitKat) onward we can re-use if the byte size of
                // the new bitmap is smaller than the reusable bitmap candidate
                // allocation byte count.
                int width = targetOptions.OutWidth / targetOptions.InSampleSize;
                int height = targetOptions.OutHeight / targetOptions.InSampleSize;

                if (targetOptions.InSampleSize > 1)
                {
                    // Android docs: the decoder uses a final value based on powers of 2, any other value will be rounded down to the nearest power of 2.
                    if (width % 2 != 0)
                      width += 1;

                    if (height % 2 != 0)
                      height += 1;
                }

                int byteCount = width * height * GetBytesPerPixel(candidate.GetConfig());
                return byteCount <= candidate.AllocationByteCount;
            }

            // On earlier versions, the dimensions must match exactly and the inSampleSize must be 1
            return candidate.Width == targetOptions.OutWidth
                    && candidate.Height == targetOptions.OutHeight
                    && targetOptions.InSampleSize == 1;
        }

        /// <summary>
        /// Return the byte usage per pixel of a bitmap based on its configuration.
        /// </summary>
        /// <param name="config">The bitmap configuration</param>
        /// <returns>The byte usage per pixel</returns>
        int GetBytesPerPixel(Bitmap.Config config)
        {
            if (config == Bitmap.Config.Argb8888)
            {
                return 4;
            }
            else if (config == Bitmap.Config.Rgb565)
            {
                return 2;
            }
            else if (config == Bitmap.Config.Argb4444)
            {
                return 2;
            }
            else if (config == Bitmap.Config.Alpha8)
            {
                return 1;
            }
            return 1;
        }

        void UpdateByteUsage(Bitmap bitmap, bool decrement = false, bool causedByEviction = false)
        {
            lock (monitor)
            {
                var byteCount = bitmap.RowBytes * bitmap.Height;
                current_cache_byte_count += byteCount * (decrement ? -1 : 1);

                // DISABLED - performance is better without it
                //if (causedByEviction)
                //{
                //	current_evicted_byte_count += byteCount;
                //	// Kick the gc if we've accrued more than our desired threshold.
                //	// TODO: Implement high/low watermarks to prevent thrashing
                //	if (current_evicted_byte_count > gc_threshold) {
                //		total_forced_gc_collections++;
                //        if (_verboseLogging)
                //		    log.Debug("Memory usage exceeds threshold, invoking GC.Collect");
                //		// Force immediate Garbage collection. Please note that is resource intensive.
                //		System.GC.Collect();
                //		System.GC.WaitForPendingFinalizers ();
                //		System.GC.WaitForPendingFinalizers (); // Double call since GC doesn't always find resources to be collected: https://bugzilla.xamarin.com/show_bug.cgi?id=20503
                //		System.GC.Collect ();
                //		current_evicted_byte_count = 0;
                //	}
                //}
            }
        }

        void OnEntryRemovedFromReusePool(object sender, EntryRemovedEventArgs<TValue> e)
        {
            ProcessRemoval(e.Value, e.Evicted);

            if (_verboseLogging && e.Evicted)
                log?.Debug("[MEMORY_CACHE] Evicted image from reuse pool " + e.Key);
        }

        void ProcessRemoval(TValue value, bool evicted)
        {
            lock (monitor)
            {
                total_removed++;

                if (!value.IsValidAndHasValidBitmap())
                    return;

                // We only really care about evictions because we do direct Remove()als
                // all the time when promoting to the displayed_cache. Only when the
                // entry has been evicted is it truly not longer being held by us.
                if (evicted)
                {
                    total_evictions++;
                    UpdateByteUsage(value.Bitmap, decrement: true, causedByEviction: true);
                    value.SetIsCached(false);
                    value.Displayed -= OnEntryDisplayed;
                    value.NoLongerDisplayed -= OnEntryNoLongerDisplayed;
                }
            }
        }

        void OnEntryNoLongerDisplayed(object sender, EventArgs args)
        {
            var sdbd = sender as TValue;
            if (sdbd != null)
            {
                lock (monitor)
                {
                    DemoteDisplayedEntryToReusePool(sdbd);
                }

                if (_verboseLogging)
                    log?.Debug("[MEMORY_CACHE] EntryNoLongerDisplayed: " + sdbd.InCacheKey);
            }
        }

        void OnEntryDisplayed(object sender, EventArgs args)
        {
            var sdbd = sender as TValue;
            if (sdbd != null)
            {
                // see if the sender is in the reuse pool and move it
                // into the displayed_cache if found.
                lock (monitor)
                {
                    PromoteReuseEntryToDisplayedCache(sdbd);
                }

                if (_verboseLogging)
                    log?.Debug("[MEMORY_CACHE] EntryDisplayed: " + sdbd.InCacheKey);
            }
        }

        void OnEntryAdded(string key, TValue value)
        {
            total_added++;
            value.SetIsCached(true);
            value.InCacheKey = key;
            value.Displayed += OnEntryDisplayed;
            UpdateByteUsage(value.Bitmap);
        }

        void PromoteReuseEntryToDisplayedCache(TValue value)
        {
            lock (monitor)
            {
                value.Displayed -= OnEntryDisplayed;
                value.NoLongerDisplayed += OnEntryNoLongerDisplayed;
                value.SetIsRetained(false);
                value.SetIsCached(true);

                reuse_pool.Remove(value.InCacheKey);
                displayed_cache.Add(value.InCacheKey, value);
            }
        }

        void DemoteDisplayedEntryToReusePool(TValue value)
        {
            lock (monitor)
            {
                value.NoLongerDisplayed -= OnEntryNoLongerDisplayed;
                value.Displayed += OnEntryDisplayed;
                value.SetIsRetained(false);
                value.SetIsCached(true);

                displayed_cache.Remove(value.InCacheKey);
                reuse_pool.Remove(value.InCacheKey);
                reuse_pool.Add(value.InCacheKey, value);
            }
        }

        public void AddToReusePool(TValue value)
        {
            lock (monitor)
            {
                value.NoLongerDisplayed -= OnEntryNoLongerDisplayed;
                value.Displayed += OnEntryDisplayed;
                value.SetIsRetained(false);
                value.SetIsCached(true);

                displayed_cache.Remove(value.InCacheKey);
                reuse_pool.Remove(value.InCacheKey);
                reuse_pool.Add(value.InCacheKey, value);
            }
        }

        public void Add(string key, TValue value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (value == null || value.Handle == IntPtr.Zero)
            {
                if (_verboseLogging)
                    log.Error("[MEMORY_CACHE] Attempt to add null value, refusing to cache");
                return;
            }

            if (!value.HasValidBitmap)
            {
                if (_verboseLogging)
                    log.Error("[MEMORY_CACHE] Attempt to add Drawable with null or recycled bitmap, refusing to cache");
                return;
            }

            lock (monitor)
            {
                Remove(key, true);
                reuse_pool.Add(key, value);
                OnEntryAdded(key, value);
            }
        }

        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            lock (monitor)
            {
                return displayed_cache.ContainsKey(key) || reuse_pool.ContainsKey(key);
            }
        }

        bool Remove(string key, bool evicted)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var result = false;

            lock (monitor)
            {
                if (displayed_cache.ContainsKey(key))
                {
                    TValue tmp = null;
                    tmp = displayed_cache.Remove(key) as TValue;

                    if (evicted)
                        reuse_pool.Remove(key);

                    ProcessRemoval(tmp, evicted: evicted);
                    result = true;
                }

                return result;
            }
        }

        public bool Remove(string key)
        {
            return Remove(key, false);
        }

        public bool TryGetValue(string key, out TValue value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = default(TValue);
                return false;
            }

            lock (monitor)
            {
                bool result = displayed_cache.TryGetValue(key, out value);
                if (result)
                {
                    reuse_pool.Get(key); // If key is found, its place in the LRU is refreshed
                    total_cache_hits++;
                    if (_verboseLogging)
                        log.Debug("[MEMORY_CACHE] Cache hit for key: " + key);
                }
                else
                {
                    TValue tmp = null;
                    result = reuse_pool.TryGetValue(key, out tmp); // If key is found, its place in the LRU is refreshed
                    if (result)
                    {
                        if (_verboseLogging)
                            log.Debug("[MEMORY_CACHE] Cache hit from reuse pool for key: " + key);
                        total_cache_hits++;
                    }
                    value = tmp;
                }
                return result;
            }
        }

        public void Clear()
        {
            lock (monitor)
            {
                var keys = displayed_cache.Keys.ToList();
                foreach (var k in keys)
                {
                    Remove(k);
                }

                displayed_cache.Clear();
                reuse_pool.Clear();
            }
        }
    }
}
