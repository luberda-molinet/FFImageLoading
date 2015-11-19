using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading.DataResolver
{
    public interface IDataResolver : IDisposable
    {
        Task<ResolverImageData> GetData(string identifier, CancellationToken token);
    }
}
