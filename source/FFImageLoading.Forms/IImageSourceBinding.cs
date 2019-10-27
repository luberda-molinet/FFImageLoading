using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;

namespace FFImageLoading.Forms
{
    public interface IImageSourceBinding
    {
        ImageSource ImageSource { get; }

        string Path { get; }

        Func<CancellationToken, Task<Stream>> Stream { get; }
    }
}
