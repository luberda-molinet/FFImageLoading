using System;
using System.Threading.Tasks;
using FFImageLoading.Work;
using System.IO;

namespace FFImageLoading
{
    /// <summary>
    /// TaskParameterPlatformExtensions
    /// </summary>
    public static class TaskParameterPlatformExtensions
    {
        /// <summary>
        /// Loads the image into PNG Stream
        /// </summary>
        /// <returns>The PNG Stream async.</returns>
        /// <param name="parameters">Parameters.</param>
        public static Task<Stream> AsPNGStreamAsync(this TaskParameter parameters)
        {
            return Task.FromResult<Stream>(new MemoryStream());
        }

        /// <summary>
        /// Loads the image into JPG Stream
        /// </summary>
        /// <returns>The JPG Stream async.</returns>
        /// <param name="parameters">Parameters.</param>
        /// <param name="quality">Quality.</param>
        public static Task<Stream> AsJPGStreamAsync(this TaskParameter parameters, int quality = 80)
        {
            return Task.FromResult<Stream>(new MemoryStream());
        }
    }
}
