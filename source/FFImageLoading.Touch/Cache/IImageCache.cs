using System;
using UIKit;

namespace FFImageLoading.Cache
{
    public interface IImageCache
    {
        UIImage Get(string key);
        void Add(string key, UIImage bitmap);
		void Clear();
		void Remove(string key);
    }
}

