using System;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers
{
    public static class Delay
    {
        /// <summary>
        /// More acurate delay implementation
        /// </summary>
        /// <returns>The delay.</returns>
        /// <param name="miliseconds">Miliseconds.</param>
        public static Task DelayAsync(int miliseconds)
        {
            if (miliseconds >= 60)
                return Task.Delay(miliseconds);

            return Task.Factory.StartNew(() => 
            {
                using (var mres = new System.Threading.ManualResetEventSlim(false))
                {
                    mres.Wait(miliseconds);
                }
            }, TaskCreationOptions.PreferFairness);
        }
    }
}
