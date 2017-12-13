using System;
using System.IO;

namespace FFImageLoading
{
    internal static class GifHelper
    {
        public static bool CheckIfAnimated(Stream st)
        {
            try
            {
                int headerCount = 0;
                bool sequenceStartCand = false;
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
