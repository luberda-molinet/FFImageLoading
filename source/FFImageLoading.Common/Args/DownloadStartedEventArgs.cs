using System;

namespace FFImageLoading.Args
{
    public class DownloadStartedEventArgs : EventArgs
    {
        public DownloadStartedEventArgs(DownloadInformation downloadInformation)
        {
            DownloadInformation = downloadInformation;
        }

        public DownloadInformation DownloadInformation { get; private set; }
    }
}
