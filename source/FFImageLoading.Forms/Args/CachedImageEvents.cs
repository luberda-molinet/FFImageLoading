using System;
using FFImageLoading.Work;

namespace FFImageLoading.Forms
{
    public static class CachedImageEvents
    {
        public class ErrorEventArgs : FFImageLoading.Args.ErrorEventArgs
        {
            public ErrorEventArgs(Exception exception) : base(exception) { }
        }

        public class SuccessEventArgs : FFImageLoading.Args.SuccessEventArgs
        {
            public SuccessEventArgs(ImageInformation imageInformation, LoadingResult loadingResult) : base(imageInformation, loadingResult) { }
        }

        public class FinishEventArgs : FFImageLoading.Args.FinishEventArgs
        {
            public FinishEventArgs(IScheduledWork scheduledWork) : base(scheduledWork) { }
        }

        public class DownloadStartedEventArgs : FFImageLoading.Args.DownloadStartedEventArgs
        {
            public DownloadStartedEventArgs(DownloadInformation downloadInformation) : base(downloadInformation) { }
        }

        public class DownloadProgressEventArgs : FFImageLoading.Args.DownloadProgressEventArgs
        {
            public DownloadProgressEventArgs(DownloadProgress downloadProgress) : base(downloadProgress) { }
        }

        public class FileWriteFinishedEventArgs : FFImageLoading.Args.FileWriteFinishedEventArgs
        {
            public FileWriteFinishedEventArgs(FileWriteInfo fileWriteInfo) : base(fileWriteInfo) { }
        }
    }
}

