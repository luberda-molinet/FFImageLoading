using Java.Lang;

namespace FFImageLoading.Cache
{
	public interface ILruCache<TValue>
		where TValue : Object
	{
		TValue Get(string key);
		void Put(string key, TValue value);
	}
}