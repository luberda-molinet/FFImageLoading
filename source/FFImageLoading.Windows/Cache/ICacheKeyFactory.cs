using System;

namespace FFImageLoading.Cache
{
	public interface ICacheKeyFactory
	{
		string GetKey(string imageSource, object dataContext);
	}
}

