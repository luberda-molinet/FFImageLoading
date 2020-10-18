using System;
using FFImageLoading.Drawables;

namespace FFImageLoading.Cache
{
    public class LRUCache : Android.Util.LruCache
    {
        public LRUCache(int maxSize) : base(maxSize)
        {
        }

        public event EventHandler<EntryRemovedEventArgs<Java.Lang.Object>> OnEntryRemoved;

        protected override int SizeOf(Java.Lang.Object key, Java.Lang.Object value)
        {

            if (value is ISelfDisposingBitmapDrawable drawable)
                return drawable.SizeInBytes;

            return 0;
        }

        protected override void EntryRemoved(bool evicted, Java.Lang.Object key, Java.Lang.Object oldValue, Java.Lang.Object newValue)
        {
            base.EntryRemoved(evicted, key, oldValue, newValue);
            OnEntryRemoved?.Invoke(this, new EntryRemovedEventArgs<Java.Lang.Object>(key.ToString(), oldValue, evicted));
        }
    }

    public class EntryRemovedEventArgs<TValue> : EventArgs
    {
        public EntryRemovedEventArgs(string key, TValue value, bool evicted)
        {
            Key = key;
            Value = value;
            Evicted = evicted;
        }

        public bool Evicted;
        public string Key;
        public TValue Value;
    }

    public class EntryAddedEventArgs<TValue> : EventArgs
    {
        public EntryAddedEventArgs(string key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public string Key;
        public TValue Value;
    }
}
