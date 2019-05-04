using System.IO;
using System;

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

        public static bool CheckIfAnimated(Stream st)
        {
            try
            {
                var headerCount = 0;
                var sequenceStartCand = false;
                int readByte;
                while ((readByte = st.ReadByte()) >= 0)
                {
                    if (readByte == 0x00)
                    {
                        sequenceStartCand = true;
                        continue;
                    }

                    //header made up of:
                    // * a static 4-byte sequence (\x00\x21\xF9\x04)
                    // * 4 variable bytes
                    // * a static 2-byte sequence (\x00\x2C) (some variants may use \x00\x21 ?)
                    if (sequenceStartCand && readByte == 0x21 && st.ReadByte() == 0xF9 && st.ReadByte() == 0x04
                        && st.ReadByte() != -1 && st.ReadByte() != -1 && st.ReadByte() != -1 && st.ReadByte() != -1
                        && st.ReadByte() == 0x00)
                    {
                        readByte = st.ReadByte();
                        if (readByte == 0x2C || readByte == 0x21)
                        {
                            headerCount++;

                            if (headerCount > 1)
                                return true;
                        }
                    }
                    else if (readByte == 0x00)
                    {
                        sequenceStartCand = true;
                        continue;
                    }

                    sequenceStartCand = false;
                }

                return false;
            }
            finally
            {
                st.Position = 0;
            }
        }
    }
}
