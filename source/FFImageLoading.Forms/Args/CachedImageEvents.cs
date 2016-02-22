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
                ImageSize = ImageInformation == null ? 
                    new ImageSize() : new ImageSize(imageInformation.OriginalWidth, imageInformation.OriginalHeight);
				LoadingResult = loadingResult;
			}

            [Obsolete("Use ImageInformation property instead")]
			public ImageSize ImageSize { get; private set; }

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
	}
}

