using System;
using System.Threading;
using FFImageLoading.Helpers;
using System.Threading.Tasks;
using System.Linq;

namespace FFImageLoading.Work
{
	public abstract class ImageLoaderTaskBase: IImageLoaderTask
	{
		protected ImageLoaderTaskBase(IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, TaskParameter parameters)
		{
			Parameters = parameters;
			MainThreadDispatcher = mainThreadDispatcher;
			Logger = miniLogger;
			ConfigureParameters();
		}

		/// <summary>
		/// Gets the parameters used to retrieve the image.
		/// </summary>
		/// <value>The parameters to retrieve the image.</value>
		public TaskParameter Parameters { get; protected set; }

		protected IMainThreadDispatcher MainThreadDispatcher { get; private set; }

		protected IMiniLogger Logger { get; private set; }

		protected CancellationTokenSource CancellationToken { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FFImageLoading.Work.ImageLoaderTaskBase"/> is completed.
		/// </summary>
		/// <value><c>true</c> if completed; otherwise, <c>false</c>.</value>
		public bool Completed { get; set; }

		/// <summary>
		/// Gets the cache key for this image loading task.
		/// </summary>
		/// <value>The cache key.</value>
		public virtual string GetKey(string path = null)
		{
			path = path ?? Parameters.Path;
			if (string.IsNullOrWhiteSpace(path))
				return null; // If path is null then something is wrong, we should not append transformations key

			return path + TransformationsKey;
		}

		public void Cancel()
		{
			CancellationToken.Cancel();
			Logger.Debug(string.Format("Canceled image generation for {0}", GetKey()));
		}

		public bool IsCancelled
		{
			get
			{
				return CancellationToken.IsCancellationRequested;
			}
		}

		/// <summary>
		/// Prepares the instance before it runs.
		/// </summary>
		public abstract Task PrepareAsync();

		/// <summary>
		/// Cancel current task only if needed
		/// </summary>
		public void CancelIfNeeded()
		{
			if (!IsCancelled && !this.Completed)
				Cancel();
		}

		public abstract Task RunAsync();

		/// <summary>
		/// Tries to load requested image from the cache asynchronously.
		/// </summary>
		/// <returns>A boolean indicating if image was loaded from cache.</returns>
		public abstract Task<bool> TryLoadingFromCacheAsync();

		protected string TransformationsKey
		{
			get
			{
				if (Parameters.Transformations == null || Parameters.Transformations.Count == 0)
					return "";

				return ";" + Parameters.Transformations.Select(t => t.Key).Aggregate((a, b) => a + ";" + b);
			}
		}

		private void ConfigureParameters()
		{
			var successCallback = Parameters.OnSuccess;
			var errorCallback = Parameters.OnError;
			var finishCallback = Parameters.OnFinish;

			// make sure callbacks are invoked on Main thread
			Parameters.Success((w, h) => MainThreadDispatcher.Post(() => successCallback(w, h)));
			Parameters.Error(ex => MainThreadDispatcher.Post(() => errorCallback(ex)));
			Parameters.Finish(scheduledWork => MainThreadDispatcher.Post(() => finishCallback(scheduledWork)));

			if (Parameters.RetryCount > 0)
			{
				int retryCurrentValue = 0;
				Parameters = Parameters.Error(async ex =>
					{
						if (retryCurrentValue < Parameters.RetryCount)
						{
                            Logger.Debug(string.Format("Retry loading operation for key {0}, trial {1}", GetKey(), retryCurrentValue));
							if (Parameters.RetryDelayInMs > 0)
								await Task.Delay(Parameters.RetryDelayInMs).ConfigureAwait(false);
							await RunAsync().ConfigureAwait(false);
							retryCurrentValue++;
						}
						errorCallback(ex);
					});
			}
		}
	}
}

