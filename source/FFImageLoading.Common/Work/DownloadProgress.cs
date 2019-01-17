using System;
namespace FFImageLoading
{
    public readonly struct DownloadProgress
    {
        public DownloadProgress(int current, int total)
        {
            Current = current;
            Total = total;
        }

        public readonly int Current;

        public readonly int Total;
    }
}
