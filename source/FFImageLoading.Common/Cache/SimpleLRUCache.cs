using System;
using System.Collections.Generic;

namespace FFImageLoading.Cache
{
	public class SimpleLRUCache<TKey, TValue>
	{
		private readonly object _lock = new object();
		private readonly int _capacity;
		private readonly LinkedList<CacheItem<TKey, TValue>> _lru = new LinkedList<CacheItem<TKey, TValue>>();
		private readonly Dictionary<TKey, LinkedListNode<CacheItem<TKey, TValue>>> _cache = new Dictionary<TKey, LinkedListNode<CacheItem<TKey, TValue>>>();

		public SimpleLRUCache(int capacity)
		{
			if (capacity < 1)
				throw new ArgumentException("Capacity must be greater than zero.");

			_capacity = capacity;
		}

		public void AddOrReplace(TKey key, TValue value)
		{
			lock(_lock)
			{
				if (_cache.TryGetValue(key, out var node))
				{
					node.Value.Value = value;
					_lru.Remove(node);
				}
				else
				{
					if (_capacity == _lru.Count)
					{
						var lastNode = _lru.First;
						_cache.Remove(lastNode.Value.Key);
						_lru.RemoveFirst();
					}

					node = new LinkedListNode<CacheItem<TKey, TValue>>(new CacheItem<TKey, TValue>(key, value));
					_cache.Add(key, node);
				}

				_lru.AddLast(node);
			}
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			lock (_lock)
			{
				if (_cache.TryGetValue(key, out var node))
				{
					_lru.Remove(node);
					_lru.AddLast(node);

					value = node.Value.Value;
					return true;
				}

				value = default;
				return false;
			}
		}

		private class CacheItem<K, V>
		{
			public K Key { get; }
			public V Value { get; set; }

			public CacheItem(K key, V value)
			{
				this.Key = key;
				this.Value = value;
			}
		}
	}
}
