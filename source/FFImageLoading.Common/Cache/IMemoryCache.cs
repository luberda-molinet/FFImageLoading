using System;
using FFImageLoading.Work;

namespace FFImageLoading.Cache
{
	public interface IMemoryCache<TImageContainer>
	{
		Tuple<TImageContainer, ImageInformation> Get(string key);

		void Add(string key, ImageInformation imageInformation, TImageContainer bitmap);

		void Clear();

		void Remove(string key);
	}
}

