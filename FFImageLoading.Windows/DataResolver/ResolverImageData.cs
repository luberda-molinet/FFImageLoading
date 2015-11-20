using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.DataResolver
{
    public class ResolverImageData
    {
        public byte[] Data { get; set; }
        public LoadingResult Result { get; set; }
        public string ResultIdentifier { get; set; }
    }
}
