using System;
using System.Diagnostics;

namespace FFImageLoading.Helpers
{
    internal class MiniLogger: IMiniLogger
    {
        public void Debug(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void Error(string errorMessage, Exception ex)
        {
            Console.WriteLine(errorMessage + Environment.NewLine + ex.ToString());
        }
    }
}

