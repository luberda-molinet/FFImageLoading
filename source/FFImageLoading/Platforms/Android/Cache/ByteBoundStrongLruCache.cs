using System;
using System.Collections.Generic;
using System.Linq;
using FFImageLoading.Drawables;

namespace FFImageLoading.Cache
{
    public class ByteBoundStrongLruCache<TValue> where TValue : Java.Lang.Object, ISelfDisposingBitmapDrawable
    {
        protected object _monitor = new object();
        private readonly LRUCache _androidCache;
        private readonly HashSet<string> _keysCache = new HashSet<string>();

        public ByteBoundStrongLruCache(int maxSize)
        {
            if (maxSize == 0)
                throw new ArgumentException("maxSize must be > 0");

            _androidCache = new LRUCache(maxSize);
            _androidCache.OnEntryRemoved += AndroidCache_OnEntryRemoved;
        }

        public event EventHandler<EntryRemovedEventArgs<TValue>> EntryRemoved;
        public event EventHandler<EntryAddedEventArgs<TValue>> EntryAdded;

        private void AndroidCache_OnEntryRemoved (object sender, EntryRemovedEventArgs<Java.Lang.Object> e)
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
                TryGetValue(key, out var outValue);
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
            return value.SizeInBytes;
        }
    }
}

