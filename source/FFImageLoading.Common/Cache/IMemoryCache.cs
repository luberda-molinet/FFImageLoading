using System;
using FFImageLoading.Work;

namespace FFImageLoading.Cache
{
    [Preserve(AllMembers = true)]
	public interface IMemoryCache<TImageContainer>
	{
		ImageInformation GetInfo(string key);

		Tuple<TImageContainer, ImageInformation> Get(string key);

		void Add(string key, ImageInformation imageInformation, TImageContainer bitmap);

		void Clear();

		void Remove(string key);

		void RemoveSimilar(string baseKey);
	}
}

