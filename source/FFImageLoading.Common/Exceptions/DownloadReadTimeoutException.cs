using System;
namespace FFImageLoading.Exceptions
{
    public class DownloadReadTimeoutException : Exception
    {
        public DownloadReadTimeoutException() : base("Read timeout")
        {
        }
    }
}
