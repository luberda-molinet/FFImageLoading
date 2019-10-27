using System;
namespace FFImageLoading.Exceptions
{
    public class DownloadException : Exception
    { 
        public DownloadException(string message) : base(message)
        {
        }
    }
}
