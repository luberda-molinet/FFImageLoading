using System;

namespace FFImageLoading.Exceptions
{
    [Preserve(AllMembers = true)]
    public class DownloadHeadersTimeoutException : Exception
    {
        public DownloadHeadersTimeoutException() : base("Headers timeout")
        {
        }
    }
}
