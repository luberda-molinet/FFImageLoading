using System;
using System.Diagnostics;

namespace HGR.Mobile.Droid.ImageLoading.Helpers
{
    internal static class MiniLogger
    {
        [Conditional("DEBUG")]
        public static void Debug(string message)
        {
            Console.WriteLine(message);
        }

        public static void Error(string errorMessage, Exception ex)
        {
            Console.WriteLine(errorMessage + Environment.NewLine + ex.ToString());
        }
    }
}

