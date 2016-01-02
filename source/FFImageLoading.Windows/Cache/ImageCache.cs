using System;
using System.Collections.Concurrent;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Cache
{
    class ImageCache : IImageCache
    {
        private static IImageCache _instance;
        private readonly ConcurrentDictionary<string, WeakReference<WriteableBitmap>> _reusableBitmaps;

        private ImageCache(int maxCacheSize)
        {
            _reusableBitmaps = new ConcurrentDictionary<string, WeakReference<WriteableBitmap>>();
        }

        public static IImageCache Instance
        {
            get
            {
                return _instance ?? (_instance = new ImageCache(ImageService.Config.MaxCacheSize));
            }
        }

        public void Add(string key, WriteableBitmap bitmap)
        {
            var weakRef = new WeakReference<WriteableBitmap>(bitmap);
            _reusableBitmaps.TryAdd(key, weakRef);
        }


        public WriteableBitmap Get(string key)
        {
            CleanAbandonedItems();

            WeakReference<WriteableBitmap> weakRef;

            if (_reusableBitmaps.TryGetValue(key, out weakRef))
            {
                WriteableBitmap bitmap = null;
                weakRef.TryGetTarget(out bitmap);
                return bitmap;
            }
            else
            {
                return null;
            }
        }

        void CleanAbandonedItems()
        {
            foreach (var item in _reusableBitmaps)
            {
                WriteableBitmap bitmap = null;
                if (!item.Value.TryGetTarget(out bitmap) || bitmap == null)
                {
                    WeakReference<WriteableBitmap> removed;
                    _reusableBitmaps.TryRemove(item.Key, out removed);
                }      
            }
        }

        public void Clear()
        {
            _reusableBitmaps.Clear();

            // Force immediate Garbage collection. Please note that is resource intensive.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void Remove(string key)
        {
            WeakReference<WriteableBitmap> removed;
            _reusableBitmaps.TryRemove(key, out removed);
        }
    }
}
