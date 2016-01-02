#if SILVERLIGHT
using System.Windows.Media.Imaging;
#else
using Windows.UI.Xaml.Media.Imaging;
#endif


namespace FFImageLoading.Cache
{
    interface IImageCache : IMemoryCache<WriteableBitmap>
    {
    }
}
