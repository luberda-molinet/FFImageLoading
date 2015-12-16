using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace FFImageLoading.Collections
{
    public class EntryRemovedEventArgs<TKey, TValue> : EventArgs
    {
        public bool Evicted;
        public TKey Key;
        public TValue OldValue;
        public TValue NewValue;
    }

    public class EntryAddedEventArgs<TKey, TValue> : EventArgs
    {
        public TKey Key;
        public TValue Value;
    }

    public class StrongLruCache<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly object sync = new object();

        private const string TAG = "StrongLruCache";

        private readonly int max_size;

        private readonly IDictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
        private readonly LinkedList<TKey> list = new LinkedList<TKey>();

        public event EventHandler<EntryRemovedEventArgs<TKey, TValue>> EntryRemoved;
        public event EventHandler<EntryAddedEventArgs<TKey, TValue>> EntryAdded;

        public StrongLruCache()
        {
        }

        public StrongLruCache(int maxSize)
        {
            max_size = maxSize;
        }

        public TValue this[TKey key]
        {
            get {
                TValue value;
                if (TryGetValue (key, out value)) {
                    return value;
                }
                else {
                    throw new KeyNotFoundException(String.Format("Key not found: {0}", key));
                }
            }

            set {
                Add (key, value, true);
            }
        }

        protected virtual bool CheckEvictionRequired()
        {
            return Count > max_size;
        }

        protected virtual void OnWillEvictEntry(TKey key, TValue value) {}

        protected virtual void OnEntryRemoved(bool evicted, TKey key, TValue oldValue, TValue newValue)
        {
            var h = EntryRemoved;
            if (h != null) {
                h(this, new EntryRemovedEventArgs<TKey, TValue> {
                    Evicted = evicted,
                    Key = key,
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
        }

        protected virtual void OnEntryAdded(TKey key, TValue value)
        {
            var h = EntryAdded;
            if (h != null) {
                h(this, new EntryAddedEventArgs<TKey, TValue> {
                    Key = key,
                    Value = value
                });
            }
        }

        /// <summary>
        /// Returns the value for the key if it exists in the cache.
        /// It does not refresh the LRU order of the returned entry.
        /// </summary>
        /// <param name="key">Key.</param>
        public TValue Peek(TKey key)
        {
            var outValue = default(TValue);
            dict.TryGetValue(key, out outValue);
            return outValue;
        }

        /// <summary>
        /// Add the specified key and value. This will overwrite the value if the key
        /// already exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void Add(TKey key, TValue value)
        {
            Add(key, value, true);
        }

        private void Add(TKey key, TValue value, bool overwrite)
        {
            var raiseEntryRemovedDueToEviction = false;
            IDictionary<TKey, TValue> evictions = null;
            var valueOverwritten = false;
            var overwrittenKey = default(TKey);
            var overwrittenValue = default(TValue);

            lock (this.sync) {
                if (dict.ContainsKey(key)) {
                    list.Remove(key);
                    if (overwrite) {
                        overwrittenKey = key;
                        overwrittenValue = dict[key];
                        dict[key] = value;
                        valueOverwritten = true;
                    }
                } else {
                    dict.Add(key, value);
                }

                list.AddLast(key);

                while (CheckEvictionRequired()) {
                    OnWillEvictEntry(list.First.Value, dict[list.First.Value]);

                    evictions = evictions ?? new Dictionary<TKey, TValue>();
                    evictions[list.First.Value] = dict[list.First.Value];

                    dict.Remove(list.First.Value);
                    list.RemoveFirst();

                    raiseEntryRemovedDueToEviction = true;
                }
            }

            OnEntryAdded(key, value);

            if (raiseEntryRemovedDueToEviction) {
                foreach (var k in evictions.Keys) {
                    OnEntryRemoved(true, k, evictions[k], default(TValue));
                }
            }
            if (valueOverwritten) {
                OnEntryRemoved(false, overwrittenKey, overwrittenValue, value);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (this.sync) {
                if (dict.TryGetValue(key, out value)) {
                    list.Remove(key);
                    list.AddLast(key);

                    return true;
                }

                return false;
            }
        }

        public void Clear()
        {
            TKey[] keys = null;
            lock (this.sync) {
                var keyCount = dict.Keys.Count;
                keys = new TKey[keyCount];
                dict.Keys.CopyTo(keys, 0);
            }
            if (keys != null) {
                foreach (var k in keys) {
                    Remove(k);
                }
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (this.sync) {
                return dict.ContainsKey(key);
            }
        }

        public bool Remove(TKey key)
        {
            TValue value = default(TValue);
            bool removed = false;
            lock (this.sync) {
                if (dict.ContainsKey(key)) {
                    value = dict[key];
                }
                list.Remove(key);
                removed = dict.Remove(key);
            }
            if (removed) {
                OnEntryRemoved(false, key, value, default(TValue));
            }
            return removed;
        }

        public ICollection<TKey> Keys {
            get {
                lock (this.sync) {
                    return list;
                }
            }
        }

        public ICollection<TValue> Values {
            get {
                lock (this.sync) {
                    return dict.Values;
                }
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (this.sync) {
                return ContainsKey(item.Key);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (this.sync) {
                dict.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public int Count {
            get {
                lock (this.sync) {
                    return dict.Count;
                }
            }
        }

        public bool IsReadOnly {
            get {
                return false;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (this.sync) {
                return dict.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (this.sync) {
                return dict.GetEnumerator();
            }
        }
    }
}

