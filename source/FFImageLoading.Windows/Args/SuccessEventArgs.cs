using FFImageLoading.Work;
using System;

namespace FFImageLoading.Args
{
    public class SuccessEventArgs : EventArgs
    {
        public SuccessEventArgs(ImageSize imageSize, LoadingResult loadingResult)
        {
            ImageSize = imageSize;
            LoadingResult = loadingResult;
        }

        public ImageSize ImageSize { get; private set; }

        public LoadingResult LoadingResult { get; private set; }
    }
}
