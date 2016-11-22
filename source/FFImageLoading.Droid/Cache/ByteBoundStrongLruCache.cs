using System;
using System.Collections.Generic;
using System.Linq;
using FFImageLoading.Drawables;

namespace FFImageLoading.Cache
{
    public class ByteBoundStrongLruCache<TValue> where TValue : Java.Lang.Object, ISelfDisposingBitmapDrawable
    {
        protected object _monitor = new object();
        LRUCache _androidCache;
        HashSet<string> _keysCache = new HashSet<string>();

        readonly int _high_watermark;
        readonly int _low_watermark;
        bool _has_exceeded_high_watermark;

        public ByteBoundStrongLruCache(int highWatermark, int lowWatermark)
        {
            _androidCache = new LRUCache(highWatermark);
            _androidCache.OnEntryRemoved += AndroidCache_OnEntryRemoved;
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

        public event EventHandler<EntryRemovedEventArgs<TValue>> EntryRemoved;
        public event EventHandler<EntryAddedEventArgs<TValue>> EntryAdded;

        void AndroidCache_OnEntryRemoved (object sender, EntryRemovedEventArgs<Java.Lang.Object> e)
        {
            lock (_monitor)
            {
                _keysCache.Remove(e.Key);
            }

            OnEntryRemoved(e.Evicted, e.Key, e.Value as TValue);
        }

        public TValue Get(string key)
        {
            lock (_monitor)
            {
                var outValue = default(TValue);
                TryGetValue(key, out outValue);
                return outValue;
            }
        }

        public bool TryGetValue(string key, out TValue value)
        {
            lock (_monitor)
            {
                value = _androidCache.Get(new Java.Lang.String(key)) as TValue;
                return value != null;
            }
        }

        public bool ContainsKey(string key)
        {
            lock (_monitor)
            {
                return _keysCache.Contains(key);
            }
        }

        public void Add(string key, TValue value)
        {
            lock (_monitor)
            {
                _androidCache.Put(new Java.Lang.String(key), value);
                _keysCache.Add(key);
                OnEntryAdded(key, value);
            }
        }

        public bool Remove(string key)
        {
            lock (_monitor)
            {
                var removed = _androidCache.Remove(new Java.Lang.String(key));
                if (removed != null)
                {
                    return true;
                }

                return false;
            }
        }

        public void Clear()
        {
            lock (_monitor)
            {
                _androidCache.EvictAll();
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                lock (_monitor)
                {
                    return _keysCache;
                }
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                lock (_monitor)
                {
                    return _androidCache.Snapshot().Values.Cast<TValue>();
                }
            }
        }

        public int CacheSizeInBytes
        {
            get
            {
                lock (_monitor)
                {
                    return _androidCache.Size();
                }
            }
        }

        protected virtual void OnEntryAdded(string key, TValue value)
        {
            EntryAdded?.Invoke(this, new EntryAddedEventArgs<TValue>(key, value));
        }

        protected virtual void OnEntryRemoved(bool evicted, string key, TValue value)
        {
            EntryRemoved?.Invoke(this, new EntryRemovedEventArgs<TValue>(key, value, evicted));
        }

        protected virtual int SizeOf(TValue value)
        {
            return (int)value.SizeInBytes;
        }
    }
}

