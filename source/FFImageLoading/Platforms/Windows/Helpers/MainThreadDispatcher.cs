using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace FFImageLoading.Helpers
{
    public class MainThreadDispatcher : IMainThreadDispatcher
    {
        static DispatcherQueue? TryGetDispatcherQueue() =>
			DispatcherQueue.GetForCurrentThread() ??
			WindowStateManager.Default?.GetActiveWindow()?.DispatcherQueue;

		static bool PlatformIsMainThread =>
			TryGetDispatcherQueue()?.HasThreadAccess ?? false;


		public async void Post(Action action)
        {
            if (action == null)
                return;

			var dispatcherQueue = TryGetDispatcherQueue();

			if (dispatcherQueue == null)
				throw new InvalidOperationException("Unable to find main thread.");

			if (PlatformIsMainThread)
			{
				action();
				return;
			}
			else
			{
				var tcs = new TaskCompletionSource<object>();

				if (!dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => action()))
					throw new InvalidOperationException("Unable to queue on the main thread.");
			}
        }

        public Task PostAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            Post(() =>
            {
                try
                {
                    action?.Invoke();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public Task PostAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<bool>();
            Post(async () =>
            {
                try
                {
                    await (action?.Invoke()).ConfigureAwait(false);
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}
