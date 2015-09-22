using System;
using System.Collections.Generic;
using FFImageLoading.Cache;
using Java.Lang;

namespace FFImageLoading
{
	public class ResourceIdentifiersCache
	{

		class IntegerLruCache : LruCache<Integer> 
		{
			public IntegerLruCache(int maxSize) : base(maxSize)
			{
			}
		}

		private const int MaxSize = 64;
		private IntegerLruCache _cache;

		private ResourceIdentifiersCache()
		{
			_cache = new IntegerLruCache(MaxSize);
		}

		public void Add(string resourceName, int resourceId)
		{
			_cache.Put(resourceName, resourceId);
		}

		public void Remove(string resourceName)
		{
			_cache.Remove(resourceName);
		}

		public int? Get(string resourceName)
		{
			var integer = _cache.Get(resourceName);
			if (integer == null)
			{
				return default(int?);
			}
			return integer.IntValue ();
		}

		public void Clear()
		{
			_cache.EvictAll();
		}

		private static ResourceIdentifiersCache _instance;
		public static ResourceIdentifiersCache Instance 
		{
			get 
			{
				if (_instance == null)
				{
					_instance = new ResourceIdentifiersCache ();
				}
				return _instance;
			}
		}

	}
}

