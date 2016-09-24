using System;
using FFImageLoading.Work;

namespace FFImageLoading.Forms
{
	public static class CachedImageEvents
	{
		public class ErrorEventArgs : EventArgs
		{
			public ErrorEventArgs(Exception exception)
			{
				Exception = exception;
			}

			public Exception Exception { get; private set; }
		}

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

		public class FinishEventArgs : EventArgs
		{
			public FinishEventArgs(IScheduledWork scheduledWork)
			{
				ScheduledWork = scheduledWork;
			}

			public IScheduledWork ScheduledWork { get; private set; }
		}

		public class DownloadStartedEventArgs : EventArgs
		{
			public DownloadStartedEventArgs(DownloadInformation downloadInformation)
			{
				DownloadInformation = downloadInformation;
			}

			public DownloadInformation DownloadInformation { get; private set; }
		}
	}
}

