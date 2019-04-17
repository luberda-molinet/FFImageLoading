using System;
namespace FFImageLoading.Exceptions
{
    [Preserve(AllMembers = true)]
    public class DownloadException : Exception
    { 
        public DownloadException(string message) : base(message)
        {
        }
    }
}
