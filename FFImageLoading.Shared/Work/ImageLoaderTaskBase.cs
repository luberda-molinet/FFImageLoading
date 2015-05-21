using System;
using System.Threading;
using FFImageLoading.Helpers;
using System.Threading.Tasks;
using System.Linq;
using FFImageLoading.Cache;

namespace FFImageLoading.Work
{
	public abstract class ImageLoaderTaskBase: IImageLoaderTask
	{
		protected ImageLoaderTaskBase(IMainThreadDispatcher mainThreadDispatcher, IMiniLogger miniLogger, TaskParameter parameters)
		{
			Parameters = parameters;
			NumberOfRetryNeeded = parameters.RetryCount;
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

		public int NumberOfRetryNeeded { get; private set; }

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
			ImageService.RemovePendingTask(this);
			CancellationToken.Cancel();
			if (Parameters.OnFinish != null)
				Parameters.OnFinish(this);
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
		public abstract Task<bool> PrepareAndTryLoadingFromCacheAsync();

		/// <summary>
		/// Cancel current task only if needed
		/// </summary>
		public void CancelIfNeeded()
		{
			if (!IsCancelled && !this.Completed)
				Cancel();
		}

		public async Task RunAsync()
		{
			try
			{
				if (Completed || CancellationToken.IsCancellationRequested || ImageService.ExitTasksEarly)
					return;

				GenerateResult generatingImageSucceeded = GenerateResult.Failed;
				Exception ex = null;

				Func<Task> perform = async () =>
					{
						try
						{
							generatingImageSucceeded = await TryGeneratingImageAsync().ConfigureAwait(false);
						}
						catch (OutOfMemoryException oom)
						{
							Logger.Error("Received an OutOfMemory we will clear the cache", oom);
							ImageCache.Instance.Clear();
							ex = oom;
						}
						catch (Exception ex2)
						{
							Logger.Error("An error occured", ex);
							ex = ex2;
						}
					};

				await perform().ConfigureAwait(false);

				// Retry logic if needed
				while (generatingImageSucceeded == GenerateResult.Failed && !IsCancelled && !Completed && NumberOfRetryNeeded > 0)
				{
					int retryNumber = Parameters.RetryCount - NumberOfRetryNeeded;
					Logger.Debug(string.Format("Retry loading operation for key {0}, trial {1}", GetKey(), retryNumber));

					if (Parameters.RetryDelayInMs > 0)
						await Task.Delay(Parameters.RetryDelayInMs).ConfigureAwait(false);

					await perform().ConfigureAwait(false);
					NumberOfRetryNeeded--;
				}

				if (!IsCancelled && !Completed && generatingImageSucceeded == GenerateResult.Failed)
				{
					if (ex == null)
						ex = new Exception("FFImageLoading is unable to generate image.");

					Parameters.OnError(ex);
				}
			}
			finally
			{
				ImageService.RemovePendingTask(this);
				if (Parameters.OnFinish != null)
					Parameters.OnFinish(this);
			}
		}

		protected abstract Task<GenerateResult> TryGeneratingImageAsync();

		/// <summary>
		/// Tries to load requested image from the cache asynchronously.
		/// </summary>
		/// <returns>A boolean indicating if image was loaded from cache.</returns>
		public abstract Task<CacheResult> TryLoadingFromCacheAsync();

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
			Parameters.Finish(scheduledWork =>
				{
					MainThreadDispatcher.Post(() => finishCallback(scheduledWork));
					Parameters.Dispose(); // if Finish is called then Parameters are useless now, we can dispose them so we don't keep a reference to callbacks
				});
		}
	}
}

