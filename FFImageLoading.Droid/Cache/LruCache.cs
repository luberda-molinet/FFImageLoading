using Android.Util;
using Java.Lang;

namespace FFImageLoading.Cache
{
    public abstract class LruCache<TValue> : LruCache
		where TValue : Object
	{

		protected LruCache(int maxSize)
			: base(maxSize)
		{
            // Can't use minilogger here, we would have too many dependencies
            decimal sizeInMB = System.Math.Round((decimal)maxSize/(1024*1024), 2);
            System.Diagnostics.Debug.WriteLine(string.Format("LruCache size: {0}MB", sizeInMB));
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