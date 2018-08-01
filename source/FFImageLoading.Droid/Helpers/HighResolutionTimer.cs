using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FFImageLoading
{
    internal class HighResolutionTimer<TImageContainer>
    {
        // The number of ticks per one millisecond.
        static readonly float TickFrequency = 1000f / Stopwatch.Frequency;

        readonly IAnimatedImage<TImageContainer>[] _animatedImage;
        readonly Action<IAnimatedImage<TImageContainer>> _action;
        volatile bool _isRunning;

        public HighResolutionTimer(IAnimatedImage<TImageContainer>[] animatedImage, Action<IAnimatedImage<TImageContainer>> action)
        {
            _animatedImage = animatedImage;
            _action = action;
        }

        public bool Enabled => _isRunning;
        public int DelayOffset { get; set; }

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            Thread thread = new Thread(ExecuteTimer);
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }

        void ExecuteTimer()
        {
            float elapsed;
            int count = _animatedImage.Length;
            float nextTrigger = 0f;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                while (_isRunning)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!_isRunning) return;

                        var image = _animatedImage[i];

                        nextTrigger += (image.Delay + DelayOffset);

                        while (true)
                        {
                            if (!_isRunning) return;

                            elapsed = stopwatch.ElapsedTicks * TickFrequency;
                            float diff = nextTrigger - elapsed;
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

                        if (!_isRunning) return;

                        float delay = elapsed - nextTrigger;
                        _action.Invoke(image);
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
