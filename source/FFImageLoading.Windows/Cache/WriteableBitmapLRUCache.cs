using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.Cache
{
    public class WriteableBitmapLRUCache : LRUCache<string, Tuple<WriteableBitmap, ImageInformation>>
    {
        public WriteableBitmapLRUCache(int capacity) : base(capacity)
        {
        }

        public override int GetValueSize(Tuple<WriteableBitmap, ImageInformation> value)
        {
            if (value?.Item2 == null)
                return 0;

            return value.Item2.CurrentHeight * value.Item2.CurrentWidth * 4;
        }
    }
}
