using System;

namespace FFImageLoading.Args
{
    [Preserve(AllMembers = true)]
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(Exception exception)
        {
        	Exception = exception;
        }

        public Exception Exception { get; private set; }
    }
}
