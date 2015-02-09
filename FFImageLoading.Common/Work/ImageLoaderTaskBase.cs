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

        public TaskParameter Parameters { get; protected set; }
       
        protected IMainThreadDispatcher MainThreadDispatcher { get; private set; }

        protected IMiniLogger Logger { get; private set; }

        protected CancellationTokenSource CancellationToken { get; set; }

        public bool Completed { get; set; }

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

        public virtual void Prepare() {
        }

        public abstract Task RunAsync();

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

