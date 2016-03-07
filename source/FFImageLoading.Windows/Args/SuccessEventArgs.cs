using FFImageLoading.Work;
using System;

namespace FFImageLoading.Args
{
    public class SuccessEventArgs : EventArgs
    {
        public SuccessEventArgs(ImageInformation imageInformation, LoadingResult loadingResult)
        {
            ImageInformation = imageInformation;
            ImageSize = ImageInformation == null ?
                new ImageSize() : new ImageSize(imageInformation.OriginalWidth, imageInformation.OriginalHeight);
            LoadingResult = loadingResult;
        }

        [Obsolete("Use ImageInformation property instead")]
        public ImageSize ImageSize { get; private set; }

        public ImageInformation ImageInformation { get; private set; }

        public LoadingResult LoadingResult { get; private set; }
    }
}
