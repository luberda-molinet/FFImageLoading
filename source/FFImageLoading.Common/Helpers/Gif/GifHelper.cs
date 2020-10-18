using System.IO;
using System;
using System.Threading.Tasks;

namespace FFImageLoading.Helpers.Gif
{
    internal static class GifHelper
    {
		public static int GetValidFrameDelay(int ms)
		{
			// https://bugzilla.mozilla.org/show_bug.cgi?id=139677
			// https://nullsleep.tumblr.com/post/16524517190/animated-gif-minimum-frame-delay-browser
			if (ms < 20)
				return 100;
				
			return ms;
		}

        public static async Task<bool> CheckIfAnimatedAsync(Stream st)
        {
            try
            {
				using (var parser = new GifHeaderParser(st))
				{
					return await parser.IsAnimatedAsync().ConfigureAwait(false);
				}
            }
            finally
            {
                st.Position = 0;
            }
        }
    }
}
