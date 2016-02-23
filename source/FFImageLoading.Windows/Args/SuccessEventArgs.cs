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

        [Obsolete("Use ImageInformation property instead")]
        public ImageSize ImageSize { get; private set; }

        public ImageInformation ImageInformation { get; private set; }

        public LoadingResult LoadingResult { get; private set; }
    }
}
