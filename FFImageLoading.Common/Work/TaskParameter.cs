using System;

namespace FFImageLoading.Work
{
	public enum ImageSource
	{
		Filepath,
		Url,
		ApplicationBundle
	}

	public class TaskParameter
	{
		/// <summary>
		/// Constructs a new TaskParameter to load an image from a file.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="filepath">Path to the file.</param>
		public static TaskParameter FromFile(string filepath)
		{
			return new TaskParameter() { Source = ImageSource.Filepath, Path = filepath };
		}

		/// <summary>
		/// Constructs a new TaskParameter to load an image from a URL.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="url">URL to the file</param>
		/// <param name="cacheDuration">How long the file will be cached on disk</param>
		public static TaskParameter FromUrl(string url, TimeSpan? cacheDuration = null)
		{
			return new TaskParameter() { Source = ImageSource.Url, Path = url, CacheDuration = cacheDuration };
		}

		/// <summary>
		/// Constructsa new TaskParameter to load an image from a file from application bundle.
		/// </summary>
		/// <param name="filepath">Path to the file.</param>
		/// <returns>The new TaskParameter.</returns>
		public static TaskParameter FromApplicationBundle(string filepath)
		{
			return new TaskParameter() { Source = ImageSource.ApplicationBundle, Path = filepath };
		}

		private TaskParameter()
		{
			// default values so we don't have a null value
			OnSuccess = () =>
			{
			};

			OnError = ex =>
			{
			};

			OnFinish = scheduledWork =>
			{
			};
		}

		public ImageSource Source { get; private set; }

		public string Path { get; private set; }

		public TimeSpan? CacheDuration { get; private set; }

		public Tuple<int, int> DownSampleSize { get; private set; }

		public int RetryCount { get; private set; }

		public int RetryDelayInMs { get; private set; }

		public Action OnSuccess { get; private set; }

		public Action<Exception> OnError { get; private set; }

		public Action<IScheduledWork> OnFinish { get; private set; }

		/// <summary>
		/// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect.
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="width">Optional width parameter, if value is higher than zero it will try to downsample to this width while keeping aspect ratio.</param>
		/// <param name="height">Optional height parameter, if value is higher than zero it will try to downsample to this height while keeping aspect ratio.</param>
		public TaskParameter DownSample(int width = 0, int height = 0)
		{
			DownSampleSize = Tuple.Create(width, height);
			return this;
		}

		/// <summary>
		/// If image loading fails automatically retry it a number of times, with a specific delay.
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="retryCount">Number of retries</param>
		/// <param name="millisecondDelay">Delay in milliseconds between each trial</param>
		public TaskParameter Retry(int retryCount = 0, int millisecondDelay = 0)
		{
			RetryCount = retryCount;
			RetryDelayInMs = millisecondDelay;
			return this;
		}

		/// <summary>
		/// If image loading succeded this callback is called
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="action">Action to invoke when loading succeded.</param>
		public TaskParameter Success(Action action)
		{
			if (action == null)
				throw new Exception("Given lambda should not be null.");

			OnSuccess = action;
			return this;
		}

		/// <summary>
		/// If image loading failed this callback is called
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="action">Action to invoke when loading failed
		public TaskParameter Error(Action<Exception> action)
		{
			if (action == null)
				throw new Exception("Given lambda should not be null.");

			OnError = action;
			return this;
		}

		/// <summary>
		/// If image loading process finished, whatever the result, this callback is called
		/// </summary>
		/// <returns>The TaskParameter instance for chaining the call.</returns>
		/// <param name="action">Action to invoke when process is done
		public TaskParameter Finish(Action<IScheduledWork> action)
		{
			if (action == null)
				throw new Exception("Given lambda should not be null.");

			OnFinish = action;
			return this;
		}
	}
}

