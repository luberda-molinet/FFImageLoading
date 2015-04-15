using System;
using System.Threading;
using FFImageLoading.Helpers;
using System.Threading.Tasks;

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
        public virtual string Key {
            get {
                return Parameters.Path;
            }
        }

        public void Cancel()
        {
            CancellationToken.Cancel();
            Logger.Debug(string.Format("Canceled image generation for {0}", Key));
        }

        public bool IsCancelled {
            get {
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

        private void ConfigureParameters()
        {
            var successCallback = Parameters.OnSuccess;
            var errorCallback = Parameters.OnError;
            var finishCallback = Parameters.OnFinish;

            // make sure callbacks are invoked on Main thread
            Parameters.Success(() => MainThreadDispatcher.Post(successCallback));
            Parameters.Error(ex => MainThreadDispatcher.Post(() => errorCallback(ex)));
            Parameters.Finish(scheduledWork => MainThreadDispatcher.Post(() => finishCallback(scheduledWork)));

            if (Parameters.RetryCount > 0) {
                int retryCurrentValue = 0;
                Parameters = Parameters.Error(async ex => {
                    if (retryCurrentValue < Parameters.RetryCount) {
                        Logger.Debug(string.Format("Retry loading operation for key {0}, trial {1}", Key, retryCurrentValue));
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

