using System;
using System.Threading.Tasks;
using FFImageLoading.Helpers;

namespace FFImageLoading.Mock
{
    public class MockMainThreadDispatcher : IMainThreadDispatcher
    {
        public Task PostAsync(Action action)
        {
            action();

            return Task.FromResult(true);
        }
    }
}
