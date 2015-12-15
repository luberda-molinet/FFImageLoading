using System;

namespace FFImageLoading.Cache
{
	public interface IMemoryCache<TImageContainer>
	{
		TImageContainer Get(string key);
		void Add(string key, TImageContainer bitmap);
		void Clear();
		void Remove(string key);
	}
}

