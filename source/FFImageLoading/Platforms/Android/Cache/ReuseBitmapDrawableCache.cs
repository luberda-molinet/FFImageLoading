using System.Linq;
using System;
using Android.Graphics;
using FFImageLoading.Helpers;
using FFImageLoading.Drawables;

namespace FFImageLoading.Cache
{
    public class ReuseBitmapDrawableCache<TValue> where TValue : Java.Lang.Object, ISelfDisposingBitmapDrawable
    {
        private readonly IMiniLogger _log;
        private readonly bool _verboseLogging;
        private readonly object _monitor = new object();
        private bool _reusePoolRefillNeeded = true;
        private readonly int _bitmapPoolSize;

        private long _totalAdded;
        private long _totalRemoved;
        private long _totalReuseHits;
        private long _totalReuseMisses;
        private long _totalEvictions;
        private long _totalCacheHits;
        private long _currentCacheByteCount;
        private long _currentEvictedByteCount;
        private long _gcThreshold;

        /// <summary>
        /// Contains all entries that are currently being displayed. These entries are not eligible for
        /// reuse or eviction. Entries will be added to the reuse pool when they are no longer displayed.
        /// </summary>
        private readonly ByteBoundStrongLruCache<TValue> _displayed_cache;

        /// <summary>
        /// Contains entries that potentially available for reuse and candidates for eviction.
        /// This is the default location for newly added entries. This cache
        /// is searched along with the displayed cache for cache hits. If a cache hit is found here, its
        /// place in the LRU list will be refreshed. Items only move out of reuse and into displayed
        /// when the entry has SetIsDisplayed(true) called on it.
        /// </summary>
        private readonly ByteBoundStrongLruCache<TValue> _reuse_pool;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FFImageLoading.Cache.ReuseBitmapDrawableCache`1"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="memoryCacheSize">Memory cache size.</param>
        /// <param name="bitmapPoolSize">Bitmap pool size.</param>
        /// <param name="verboseLogging">If set to <c>true</c> verbose logging.</param>
        public ReuseBitmapDrawableCache(IMiniLogger logger, int memoryCacheSize, int bitmapPoolSize, bool verboseLogging = false)
        {
            _gcThreshold = bitmapPoolSize;
            _verboseLogging = verboseLogging;
            _log = logger;
            _bitmapPoolSize = bitmapPoolSize;
            _displayed_cache = new ByteBoundStrongLruCache<TValue>(memoryCacheSize);
            _reuse_pool = new ByteBoundStrongLruCache<TValue>(bitmapPoolSize);
            _reuse_pool.EntryRemoved += OnEntryRemovedFromReusePool;
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
            lock (_monitor)
            {
                if (_reuse_pool.CacheSizeInBytes < _bitmapPoolSize && _reusePoolRefillNeeded)
                {
                    _totalReuseMisses++;
                    return null;
                }

                _reusePoolRefillNeeded = false;
                TValue reuseDrawable = null;

                var reuse_values = _reuse_pool.Values;
                foreach (var bd in reuse_values)
                {
					if (bd is ISelfDisposingAnimatedBitmapDrawable)
						continue;

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
                    _reuse_pool.Remove(reuseDrawable.InCacheKey);
                    _totalReuseHits++;

                    if (_verboseLogging)
                        _log?.Debug("[MEMORY_CACHE] Used image from reuse pool for decoding optimization");
                }
                else
                {
                    _totalReuseMisses++;
                    // Indicate that the pool may need to be refilled.
                    // There is little harm in setting this flag since it will be unset
                    // on the next reuse request if the threshold is reuse_pool.CacheSizeInBytes >= low_watermark.
                    _reusePoolRefillNeeded = true;
                }

                return reuseDrawable;
            }
        }

        private bool CanUseForInBitmap(Bitmap candidate, BitmapFactory.Options targetOptions)
        {
            if (Utils.HasKitKat())
            {
                // From Android 4.4 (KitKat) onward we can re-use if the byte size of
                // the new bitmap is smaller than the reusable bitmap candidate
                // allocation byte count.
                var width = targetOptions.OutWidth / targetOptions.InSampleSize;
                var height = targetOptions.OutHeight / targetOptions.InSampleSize;

                if (targetOptions.InSampleSize > 1)
                {
                    // Android docs: the decoder uses a final value based on powers of 2, any other value will be rounded down to the nearest power of 2.
                    if (width % 2 != 0)
                      width += 1;

                    if (height % 2 != 0)
                      height += 1;
                }

                var byteCount = width * height * GetBytesPerPixel(candidate.GetConfig());
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
        private int GetBytesPerPixel(Bitmap.Config config)
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

        private void UpdateByteUsage(Bitmap bitmap, bool decrement = false, bool causedByEviction = false)
        {
            var byteCount = bitmap.RowBytes * bitmap.Height;
            _currentCacheByteCount += byteCount * (decrement ? -1 : 1);

            if (causedByEviction)
            {
                _currentEvictedByteCount += byteCount;
                // Kick the gc if we've accrued more than our desired threshold.
                if (_currentEvictedByteCount > _gcThreshold)
                {
                    _currentEvictedByteCount = 0;

                    if (_verboseLogging)
                        _log.Debug("[MEMORY_CACHE] Invoking GC.Collect");

                    GC.Collect();
                    Java.Lang.JavaSystem.Gc();
                }
            }
        }

        private void OnEntryRemovedFromReusePool(object sender, EntryRemovedEventArgs<TValue> e)
        {
            ProcessRemoval(e.Value, e.Evicted);

			if (e.Value is ISelfDisposingAnimatedBitmapDrawable)
				Java.Lang.JavaSystem.Gc();

			if (_verboseLogging && e.Evicted)
                _log?.Debug("[MEMORY_CACHE] Evicted image from reuse pool " + e.Key);
        }

        private void ProcessRemoval(TValue value, bool evicted)
        {
            lock (_monitor)
            {
                _totalRemoved++;

                if (!value.IsValidAndHasValidBitmap())
                    return;

                // We only really care about evictions because we do direct Remove()als
                // all the time when promoting to the displayed_cache. Only when the
                // entry has been evicted is it truly not longer being held by us.
                if (evicted)
                {
                    _totalEvictions++;
                    UpdateByteUsage(value.Bitmap, decrement: true, causedByEviction: true);
                    value.SetIsCached(false);
                    value.Displayed -= OnEntryDisplayed;
                    value.NoLongerDisplayed -= OnEntryNoLongerDisplayed;
                }
            }
        }

        private void OnEntryNoLongerDisplayed(object sender, EventArgs args)
        {
            if (sender is TValue sdbd)
            {
                lock (_monitor)
                {
                    DemoteDisplayedEntryToReusePool(sdbd);
                }

                if (_verboseLogging)
                    _log?.Debug("[MEMORY_CACHE] EntryNoLongerDisplayed: " + sdbd.InCacheKey);
            }
        }

        private void OnEntryDisplayed(object sender, EventArgs args)
        {
            if (sender is TValue sdbd)
            {
                // see if the sender is in the reuse pool and move it
                // into the displayed_cache if found.
                lock (_monitor)
                {
                    PromoteReuseEntryToDisplayedCache(sdbd);
                }

                if (_verboseLogging)
                    _log?.Debug("[MEMORY_CACHE] EntryDisplayed: " + sdbd.InCacheKey);
            }
        }

        private void OnEntryAdded(string key, TValue value)
        {
            _totalAdded++;
            value.SetIsCached(true);
            value.InCacheKey = key;
            value.Displayed += OnEntryDisplayed;
            UpdateByteUsage(value.Bitmap);
        }

        private void PromoteReuseEntryToDisplayedCache(TValue value)
        {
            lock (_monitor)
            {
				if (value == null || value.Handle == IntPtr.Zero)
					return;

				value.Displayed -= OnEntryDisplayed;
                value.NoLongerDisplayed += OnEntryNoLongerDisplayed;
                value.SetIsRetained(false);
                value.SetIsCached(true);

                _reuse_pool.Remove(value.InCacheKey);
                _displayed_cache.Add(value.InCacheKey, value);
            }
        }

        private void DemoteDisplayedEntryToReusePool(TValue value)
        {
            lock (_monitor)
            {
				if (value == null || value.Handle == IntPtr.Zero)
					return;

				value.NoLongerDisplayed -= OnEntryNoLongerDisplayed;
                value.Displayed += OnEntryDisplayed;
                value.SetIsRetained(false);
                value.SetIsCached(true);

                _displayed_cache.Remove(value.InCacheKey);
                _reuse_pool.Remove(value.InCacheKey);

				if (!string.IsNullOrEmpty(value.InCacheKey))
                	_reuse_pool.Add(value.InCacheKey, value);
            }
        }

        public void AddToReusePool(TValue value)
        {
            lock (_monitor)
            {
				if (value == null || value.Handle == IntPtr.Zero)
					return;

				value.NoLongerDisplayed -= OnEntryNoLongerDisplayed;
                value.Displayed += OnEntryDisplayed;
                value.SetIsRetained(false);
                value.SetIsCached(true);

                _displayed_cache.Remove(value.InCacheKey);
                _reuse_pool.Remove(value.InCacheKey);
                _reuse_pool.Add(value.InCacheKey, value);
            }
        }

        public void Add(string key, TValue value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (value == null || value.Handle == IntPtr.Zero)
            {
                if (_verboseLogging)
                    _log.Error("[MEMORY_CACHE] Attempt to add null value, refusing to cache");
                return;
            }

            if (!value.HasValidBitmap)
            {
                if (_verboseLogging)
                    _log.Error("[MEMORY_CACHE] Attempt to add Drawable with null or recycled bitmap, refusing to cache");
                return;
            }

            lock (_monitor)
            {
                Remove(key, true);
                _reuse_pool.Add(key, value);
                OnEntryAdded(key, value);
            }
        }

        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            lock (_monitor)
            {
                return _displayed_cache.ContainsKey(key) || _reuse_pool.ContainsKey(key);
            }
        }

        private bool Remove(string key, bool evicted)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var result = false;

            lock (_monitor)
            {
                if (evicted || _displayed_cache.ContainsKey(key))
                {
                    TValue tmp = null;
                    tmp = _displayed_cache.Remove(key) as TValue;

                    if (evicted)
                        _reuse_pool.Remove(key);

                    ProcessRemoval(tmp, evicted: evicted);
                    result = true;
                }

                return result;
            }
        }

        public bool Remove(string key)
        {
            return Remove(key, true);
        }

        public bool TryGetValue(string key, out TValue value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = default;
                return false;
            }

            lock (_monitor)
            {
                var result = _displayed_cache.TryGetValue(key, out value);
                if (result)
                {
                    _reuse_pool.Get(key); // If key is found, its place in the LRU is refreshed
                    _totalCacheHits++;
                    if (_verboseLogging)
                        _log.Debug("[MEMORY_CACHE] Cache hit for key: " + key);
                }
                else
                {
                    result = _reuse_pool.TryGetValue(key, out var tmp); // If key is found, its place in the LRU is refreshed
                    if (result)
                    {
                        if (_verboseLogging)
                            _log.Debug("[MEMORY_CACHE] Cache hit from reuse pool for key: " + key);
                        _totalCacheHits++;
                    }
                    value = tmp;
                }
                return result;
            }
        }

        public void Clear()
        {
            lock (_monitor)
            {
                var keys = _displayed_cache.Keys.ToList();
                foreach (var k in keys)
                {
                    Remove(k);
                }

                _displayed_cache.Clear();
                _reuse_pool.Clear();
            }
        }
    }
}
