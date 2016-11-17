using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FFImageLoading.Collections
{
    public abstract class LRUCache<TKey, TValue>
    {
        IDictionary<TKey, LinkedListNode<LRUCacheItem<TKey, TValue>>> cacheMap = new Dictionary<TKey, LinkedListNode<LRUCacheItem<TKey, TValue>>>();
        LinkedList<LRUCacheItem<TKey, TValue>> lruList = new LinkedList<LRUCacheItem<TKey, TValue>>();
        protected object monitor = new object();

        public TValue Get(TKey key)
        {
            lock (monitor)
            {
                var outValue = default(TValue);
                TryGetValue(key, out outValue);
                return outValue;
            }
        }

        /// <summary>
        /// Returns the value for the key if it exists in the cache.
        /// It does not refresh the LRU order of the returned entry.
        /// </summary>
        /// <param name="key">Key.</param>
        public TValue Peek(TKey key)
        {
            lock (monitor)
            {
                var outValue = default(TValue);
                LinkedListNode<LRUCacheItem<TKey, TValue>> node;
                if (cacheMap.TryGetValue(key, out node))
                {
                    outValue = node.Value.Value;
                }

                return outValue;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (monitor)
            {
                LinkedListNode<LRUCacheItem<TKey, TValue>> node;
                if (cacheMap.TryGetValue(key, out node))
                {
                    value = node.Value.Value;
                    lruList.Remove(node);
                    lruList.AddLast(node);
                    return true;
                }

                value = default(TValue);
                return false;
            }
        }

        public event EventHandler<EntryRemovedEventArgs<TKey, TValue>> EntryRemoved;
        public event EventHandler<EntryAddedEventArgs<TKey, TValue>> EntryAdded;

        public void Add(TKey key, TValue val)
        {
            lock (monitor)
            {
                while (CheckEvictionRequired())
                {
                    RemoveFirst();
                }

                LRUCacheItem<TKey, TValue> cacheItem = new LRUCacheItem<TKey, TValue>(key, val);
                LinkedListNode<LRUCacheItem<TKey, TValue>> node = new LinkedListNode<LRUCacheItem<TKey, TValue>>(cacheItem);
                lruList.AddLast(node);
                cacheMap.Add(key, node);
                OnEntryAdded(key, val);
            }
        }

        public bool Remove(TKey key)
        {
            lock (monitor)
            {
                LinkedListNode<LRUCacheItem<TKey, TValue>> node = default(LinkedListNode<LRUCacheItem<TKey, TValue>>);
                if (cacheMap.TryGetValue(key, out node))
                {
                    lruList.Remove(node);
                    cacheMap.Remove(node.Value.Key);
                    OnEntryRemoved(false, node.Value.Key, node.Value.Value);

                    return true;
                }

                return false;
            }
        }

        public void Clear()
        {
            lock (monitor)
            {
                foreach (var node in cacheMap.Values.ToList())
                {
                    lruList.Remove(node);
                    cacheMap.Remove(node.Value.Key);
                    OnEntryRemoved(false, node.Value.Key, node.Value.Value);
                }

                lruList.Clear();
                cacheMap.Clear();
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (monitor)
            {
                return cacheMap.ContainsKey(key);
            }
        }

        public int Count
        {
            get
            {
                lock (monitor)
                {
                    return cacheMap.Count;
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                lock (monitor)
                {
                    return cacheMap.Keys.ToList();
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                lock (monitor)
                {
                    return cacheMap.Values.Select(v => v.Value.Value).ToList();
                }
            }
        }

        void RemoveFirst()
        {
            // Remove from LRUPriority
            LinkedListNode<LRUCacheItem<TKey, TValue>> node = lruList.First;
            OnEntryRemoved(true, node.Value.Key, node.Value.Value);
            lruList.RemoveFirst();
            // Remove from cache
            cacheMap.Remove(node.Value.Key);
        }

        protected abstract bool CheckEvictionRequired();

        protected virtual void OnEntryAdded(TKey key, TValue value) 
        {
            EntryAdded?.Invoke(this, new EntryAddedEventArgs<TKey, TValue>(key, value));
        }

        protected virtual void OnEntryRemoved(bool evicted, TKey key, TValue value) 
        {
            EntryRemoved?.Invoke(this, new EntryRemovedEventArgs<TKey, TValue>(key, value, evicted));
        }
    }

    class LRUCacheItem<TKey, TValue>
    {
        public LRUCacheItem(TKey k, TValue v)
        {
            Key = k;
            Value = v;
        }
        public TKey Key;
        public TValue Value;
    }

    public class EntryRemovedEventArgs<TKey, TValue> : EventArgs
    {
        public EntryRemovedEventArgs(TKey key, TValue value, bool evicted)
        {
            Key = key;
            Value = value;
            Evicted = evicted;
        }

        public bool Evicted;
        public TKey Key;
        public TValue Value;
    }

    public class EntryAddedEventArgs<TKey, TValue> : EventArgs
    {
        public EntryAddedEventArgs(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public TKey Key;
        public TValue Value;
    }
}
