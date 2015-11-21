using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            if (!_reusableBitmaps.TryAdd(key, weakRef))
            {
                WeakReference<WriteableBitmap> removed;
                _reusableBitmaps.TryRemove(key, out removed);
                Add(key, bitmap);
            }
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
            List<string> deadKeys = new List<string>();

            foreach (var item in _reusableBitmaps)
            {
                WriteableBitmap bitmap = null;
                if (!item.Value.TryGetTarget(out bitmap) || bitmap == null)
                {
                    deadKeys.Add(item.Key);
                }      
            }

            foreach (var item in deadKeys)
            {
                WeakReference<WriteableBitmap> removed;
                _reusableBitmaps.TryRemove(item, out removed);
            }
        }

        public void Clear()
        {
            _reusableBitmaps.Clear();

            // Force immediate Garbage collection. Please note that is resource intensive.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForPendingFinalizers(); // Double call since GC doesn't always find resources to be collected: https://bugzilla.xamarin.com/show_bug.cgi?id=20503
            GC.Collect();
        }

        public void Remove(string key)
        {
            WeakReference<WriteableBitmap> removed;
            _reusableBitmaps.TryRemove(key, out removed);
        }
    }
}
