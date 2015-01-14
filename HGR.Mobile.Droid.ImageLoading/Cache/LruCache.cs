using Android.Util;
using Java.Lang;
using HGR.Mobile.Droid.ImageLoading.Helpers;

namespace HGR.Mobile.Droid.ImageLoading.Cache
{
	public abstract class LruCache<TValue> : LruCache, ILruCache<TValue>
		where TValue : Object
	{

		protected LruCache(int maxSize)
			: base( maxSize)
		{
			MiniLogger.Debug(string.Format("LruCache size: {0}KB", maxSize));
		}

		public virtual TValue Get(string key)
		{
			return (TValue) base.Get(key);
		}

		public void Put(string key, TValue value)
		{
			if (Get(key) == null)
				base.Put(key, value);
		}
	}
}