using System;

namespace FFImageLoading.Exceptions
{
    public class DownloadHeadersTimeoutException : Exception
    {
        public DownloadHeadersTimeoutException() : base("Headers timeout")
        {
        }
    }
}
