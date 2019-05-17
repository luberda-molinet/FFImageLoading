using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Drawables;
using Android.Graphics;

namespace FFImageLoading
{
    internal class HighResolutionTimer<TImageContainer> where TImageContainer: class
    {
#pragma warning disable RECS0108 // Warns about static fields in generic types
        // The number of ticks per one millisecond.
        private static readonly float _tickFrequency = 1000f / Stopwatch.Frequency;
#pragma warning restore RECS0108 // Warns about static fields in generic types

        private readonly Func<HighResolutionTimer<TImageContainer>, Bitmap, Task> _action;

        public HighResolutionTimer(ISelfDisposingAnimatedBitmapDrawable animatedDrawable, Func<HighResolutionTimer<TImageContainer>, Bitmap, Task> action)
        {
			AnimatedDrawable = animatedDrawable;
            _action = action;
        }

		public ISelfDisposingAnimatedBitmapDrawable AnimatedDrawable { get; private set; }
		public bool Enabled { get; private set; }
        public int DelayOffset { get; set; }

        public void Start()
        {
            if (Enabled)
                return;

            Enabled = true;

            var thread = new Thread(ExecuteTimer)
            {
                Priority = ThreadPriority.BelowNormal
            };
            thread.Start();
        }

        public void Stop()
        {
            Enabled = false;
        }

        private async void ExecuteTimer()
        {
            float elapsed;
            var count = AnimatedDrawable.AnimatedImages.Length;
            var nextTrigger = 0f;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                while (Enabled)
                {
                    for (var i = 0; i < count; i++)
                    {
                        if (!Enabled) return;

                        var image = AnimatedDrawable.AnimatedImages[i];

                        nextTrigger += (image.Delay + DelayOffset);

                        while (true)
                        {
                            if (!Enabled) return;

                            elapsed = stopwatch.ElapsedTicks * _tickFrequency;
                            var diff = nextTrigger - elapsed;
                            if (diff <= 0f)
                                break;

                            if (diff < 1f)
                                Thread.SpinWait(10);
                            else if (diff < 5f)
                                Thread.SpinWait(100);
                            else if (diff < 15f)
                                Thread.Sleep(1);
                            else
                                Thread.Sleep(10);
                        }

                        if (!Enabled) return;

                        var delay = elapsed - nextTrigger;
                        await (_action.Invoke(this, image.Image)).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
