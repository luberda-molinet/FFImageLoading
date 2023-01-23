using System;
namespace FFImageLoading.Exceptions
{
    [Preserve(AllMembers = true)]
    public class DownloadReadTimeoutException : Exception
    {
        public DownloadReadTimeoutException() : base("Read timeout")
        {
        }
    }
}
