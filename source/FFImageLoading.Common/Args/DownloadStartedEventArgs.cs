using System;

namespace FFImageLoading.Args
{
    [Preserve(AllMembers = true)]
    public class DownloadStartedEventArgs : EventArgs
    {
        public DownloadStartedEventArgs(DownloadInformation downloadInformation)
        {
            DownloadInformation = downloadInformation;
        }

        public DownloadInformation DownloadInformation { get; private set; }
    }
}
