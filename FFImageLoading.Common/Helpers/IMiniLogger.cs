using System;
using System.Diagnostics;

namespace FFImageLoading.Helpers
{
    public interface IMiniLogger
    {
        void Debug(string message);

        void Error(string errorMessage, Exception ex);
    }
}

