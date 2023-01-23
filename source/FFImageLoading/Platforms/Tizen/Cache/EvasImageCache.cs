using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FFImageLoading.Work;
using FFImageLoading.Helpers;
using FFImageLoading.Views;


namespace FFImageLoading.Cache
{
    internal class EvasImageCache : IMemoryCache<SharedEvasImage>
    {
        static Lazy<IMemoryCache<SharedEvasImage>> s_instance = new Lazy<IMemoryCache<SharedEvasImage>>(() =>
        {
            return new EvasImageCache(ImageService.Instance.Config.MaxMemoryCacheSize, ImageService.Instance.Config.Logger);
        });

        readonly ConcurrentDictionary<string, ImageInformation> _imageInformations = new ConcurrentDictionary<string, ImageInformation>();
        readonly Dictionary<string, SharedEvasImage> _cache = new Dictionary<string, SharedEvasImage>();
        readonly LinkedList<string> _lruQueue = new LinkedList<string>();
        readonly IMiniLogger _logger;
        readonly object _lock = new object();
        readonly int _maxCacheSize;
        int _currentCacheSize;

        EvasImageCache(int maxCacheSize, IMiniLogger logger)
        {
            _maxCacheSize = maxCacheSize;
            _logger = logger;

            _currentCacheSize = 0;
            if (_maxCacheSize <= 0)
            {
                _maxCacheSize = 1920 * 1080 * 10 * 4; // 10 FHD Images
            }
        }

        public static IMemoryCache<SharedEvasImage> Instance
        {
            get
            {
                return s_instance.Value;
            }
        }

        public ImageInformation GetInfo(string key)
        {
            ImageInformation imageInformation;
            if (_imageInformations.TryGetValue(key, out imageInformation))
            {
                return imageInformation;
            }
            return null;
        }

        public Tuple<SharedEvasImage, ImageInformation> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (!_imageInformations.ContainsKey(key))
                return null;

            lock (_lock)
            {
                ImageInformation imgInfo;
                SharedEvasImage img;
                if (_imageInformations.TryGetValue(key, out imgInfo) && _cache.TryGetValue(key, out img))
                {
                    _lruQueue.Remove(key);
                    _lruQueue.AddFirst(key);
                    return new Tuple<SharedEvasImage, ImageInformation>(img, imgInfo);
                }
            }
            return null;
        }

        public void Add(string key, ImageInformation imageInformation, SharedEvasImage value)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null || value.Handle == IntPtr.Zero)
                return;

            if (_imageInformations.ContainsKey(key))
                Remove(key, false);

            var byteSize = imageInformation.CurrentWidth * imageInformation.CurrentHeight * 4;

            lock (_lock)
            {
                _currentCacheSize += byteSize;
                if (_currentCacheSize > _maxCacheSize)
                {
                    if (ImageService.Instance.Config.VerboseMemoryCacheLogging)
                        _logger.Debug($"MemoryCache Size Exceed {_currentCacheSize/1024.0}kb");

                    if (_lruQueue.Last != null)
                    {
                        var removedKey = _lruQueue.Last.Value;
                        _lruQueue.RemoveLast();
                        Remove(removedKey);
                    }
                }
                value.AddRef();
                _imageInformations.TryAdd(key, imageInformation);
                _cache.Add(key, value);
                _lruQueue.AddFirst(key);
            }
        }

        public void Remove(string key)
        {
            Remove(key, true);
        }

        void Remove(string key, bool log)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (log && ImageService.Instance.Config.VerboseMemoryCacheLogging)
                _logger.Debug(string.Format($"Remove from memory cache called for {key}"));

            SharedEvasImage removedImage = null;
            lock (_lock)
            {
                ImageInformation removedImageInfo = null;
                if (_imageInformations.TryRemove(key, out removedImageInfo))
                {
                    var byteSize = removedImageInfo.CurrentWidth * removedImageInfo.CurrentHeight * 4;
                    _currentCacheSize -= byteSize;
                }
                if (_cache.TryGetValue(key, out removedImage))
                {
                    _cache.Remove(key);
                }
                _lruQueue.Remove(key);
            }
            removedImage?.RemoveRef();
        }

        public void RemoveSimilar(string baseKey)
        {
            if (string.IsNullOrWhiteSpace(baseKey))
                return;

            var pattern = baseKey + ";";

            var keysToRemove = _imageInformations.Keys.Where(i => i.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase)).ToList();
            foreach (var key in keysToRemove)
            {
                Remove(key);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _imageInformations.Clear();
                foreach (SharedEvasImage img in _cache.Values)
                {
                    img.RemoveRef();
                }
                _cache.Clear();
                _lruQueue.Clear();
                _currentCacheSize = 0;
            }

            GC.Collect();
        }
    }
}

