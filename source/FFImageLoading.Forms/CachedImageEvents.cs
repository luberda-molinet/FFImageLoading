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
			public SuccessEventArgs(ImageSize imageSize, LoadingResult loadingResult)
			{
				ImageSize = imageSize;
				LoadingResult = loadingResult;
			}

			public ImageSize ImageSize { get; private set; }

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
	}
}

