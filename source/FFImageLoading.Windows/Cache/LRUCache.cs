using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFImageLoading.Cache
{
    public abstract class LRUCache<TKey, TValue>  where TKey : class where TValue : class
    {
        private readonly object _lockObj = new object();
        private int _currentSize;
        private Dictionary<TKey, LinkedListNode<LRUCacheItem<TKey, TValue>>> _cacheMap = new Dictionary<TKey, LinkedListNode<LRUCacheItem<TKey, TValue>>>();
        protected LinkedList<LRUCacheItem<TKey, TValue>> _lruList = new LinkedList<LRUCacheItem<TKey, TValue>>();

        protected int _capacity;

        public LRUCache(int capacity)
        {
            _capacity = capacity;
        }

        public abstract int GetValueSize(TValue value);

        public bool ContainsKey(TKey key)
        {
            TValue dummy;
            return TryGetValue(key, out dummy);
        }

        public TValue Get(TKey key)
        {
            lock (_lockObj)
            {
                LinkedListNode<LRUCacheItem<TKey, TValue>> node;
                if (_cacheMap.TryGetValue(key, out node))
                {
                    TValue value = node.Value.Value;
                    _lruList.Remove(node);
                    _lruList.AddLast(node);
                    return value;
                }
                return default(TValue);
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            lock (_lockObj)
            {
                CleanAbandonedItems();

                if (_cacheMap.ContainsKey(key))
                {
                    return false;
                }
                this.CheckSize(key, value);
                LRUCacheItem<TKey, TValue> cacheItem = new LRUCacheItem<TKey, TValue>(key, value);
                LinkedListNode<LRUCacheItem<TKey, TValue>> node =
                    new LinkedListNode<LRUCacheItem<TKey, TValue>>(cacheItem);
                _lruList.AddLast(node);
                _cacheMap.Add(key, node);

                return true;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lockObj)
            {
                LinkedListNode<LRUCacheItem<TKey, TValue>> node;
                if (_cacheMap.TryGetValue(key, out node))
                {
                    value = node.Value.Value;

                    if (value == null)
                    {
                        Remove(key);
                        return false;
                    }

                    _lruList.Remove(node);
                    _lruList.AddLast(node);
                    return true;
                }
                value = default(TValue);
                return false;
            }
        }

        public void Clear()
        {
            lock (_lockObj)
            {
                _cacheMap.Clear();
                _lruList.Clear();
            }
        }

        public IList<TKey> Keys
        {
            get
            {
                lock (_lockObj)
                {
                    return _cacheMap.Keys.ToList();
                }
            }
        }

        public IList<TValue> Values
        {
            get
            {
                lock (_lockObj)
                {
                    return _cacheMap.Values.Select(v => v.Value.Value).ToList();
                }
            }
        }

        void CleanAbandonedItems()
        {
            //TODO?
        }

        protected virtual bool CheckSize(TKey key, TValue value)
        {
            var size = GetValueSize(value);
            _currentSize += size;

            while (_currentSize > _capacity && _lruList.Count > 0)
            {
                this.RemoveFirst();
            }

            return true;
        }

        public void Remove(TKey key)
        {
            LinkedListNode<LRUCacheItem<TKey, TValue>> node;
            if (_cacheMap.TryGetValue(key, out node))
            {
                _lruList.Remove(node);
            }
        }

        protected virtual void RemoveNode(LinkedListNode<LRUCacheItem<TKey, TValue>> node)
        {
            _lruList.Remove(node);
            _cacheMap.Remove(node.Value.Key);
            _currentSize -= GetValueSize(node.Value.Value);
        }

        protected void RemoveFirst()
        {
            LinkedListNode<LRUCacheItem<TKey, TValue>> node = _lruList.First;
            this.RemoveNode(node);
        }

        protected class LRUCacheItem<K, V>
        {
            public LRUCacheItem(K k, V v)
            {
                Key = k;
                Value = v;
            }
            public K Key
            {
                get;
                private set;
            }
            public V Value
            {
                get;
                private set;
            }
        }
    }
}
