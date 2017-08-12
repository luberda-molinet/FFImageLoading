using System;
using FFImageLoading.Work;

namespace FFImageLoading.Args
{
    [Preserve(AllMembers = true)]
    public class SuccessEventArgs : EventArgs
    {
        public SuccessEventArgs(ImageInformation imageInformation, LoadingResult loadingResult)
        {
            ImageInformation = imageInformation;
            LoadingResult = loadingResult;
        }

        public ImageInformation ImageInformation { get; private set; }

        public LoadingResult LoadingResult { get; private set; }
    }
}
