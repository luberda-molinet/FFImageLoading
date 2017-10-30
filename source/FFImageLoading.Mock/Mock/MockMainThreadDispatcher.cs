using System;
using System.Threading.Tasks;
using FFImageLoading.Helpers;

namespace FFImageLoading.Mock
{
    public class MockMainThreadDispatcher : IMainThreadDispatcher
    {
        public void Post(Action action)
        {
            action();
        }

        public Task PostAsync(Action action)
        {
            action();

            return Task.FromResult(true);
        }

        public Task PostAsync(Func<Task> action)
        {
            return action?.Invoke();
        }
    }
}
