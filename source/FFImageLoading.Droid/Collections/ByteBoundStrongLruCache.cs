using System;

namespace FFImageLoading.Collections
{
	public interface IByteSizeAware
	{
		long SizeInBytes { get; }
	}

    public class ByteBoundStrongLruCache<TKey, TValue> : LRUCache<TKey, TValue> where TValue : IByteSizeAware
    {
        const string TAG = "ByteBoundStrongLruCache";
        readonly long _high_watermark;
        readonly long _low_watermark;
        long _current_cache_size;
        bool _has_exceeded_high_watermark;

        public ByteBoundStrongLruCache(long highWatermark, long lowWatermark)
        {
            _high_watermark = Math.Max(0, highWatermark);
            _low_watermark = Math.Max(0, lowWatermark);
            if (_high_watermark == 0)
            {
                throw new ArgumentException("highWatermark must be > 0");
            }
            if (_low_watermark == 0)
            {
                throw new ArgumentException("lowWatermark must be > 0");
            }
            if (_high_watermark < _low_watermark)
            {
                _high_watermark = _low_watermark;
            }
        }

        public long CacheSizeInBytes 
        {
            get 
            { 
                lock (monitor) 
                { 
                    return _current_cache_size; 
                } 
            }
        }

        protected override void OnEntryAdded(TKey key, TValue value)
        {
            lock (monitor)
            {
                _current_cache_size += value.SizeInBytes;
                base.OnEntryAdded(key, value);
            }
        }

        protected override void OnEntryRemoved(bool evicted, TKey key, TValue value)
        {
            lock(monitor)
            {
                _current_cache_size -= value.SizeInBytes;
                base.OnEntryRemoved(evicted, key, value);
            }
        }

        protected override bool CheckEvictionRequired()
        {
            lock (monitor)
            {
                if (_current_cache_size > _high_watermark)
                {
                    _has_exceeded_high_watermark = true;
                    return true;
                }

                //if (_has_exceeded_high_watermark && _current_cache_size > _low_watermark)
                //{
                //    return true;
                //}
                //_has_exceeded_high_watermark = false;

                return false;
            }
        }
    }
}

