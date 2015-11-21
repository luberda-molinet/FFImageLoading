using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Cache
{
    interface IImageCache
    {
        WriteableBitmap Get(string key);
        void Add(string key, WriteableBitmap bitmap);
        void Remove(string key);
        void Clear();
    }
}
