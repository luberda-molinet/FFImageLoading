using System;
using System.IO;
using FFImageLoading.Helpers;
using System.Text;

namespace FFImageLoading.Mock
{
    public class MockMD5Helper : IMD5Helper
    {
        public MockMD5Helper()
        {
        }

        public string MD5(string input)
        {
            return input.GetHashCode().ToString();
        }

        public string MD5(Stream stream)
        {
            StreamReader input = new StreamReader(stream, Encoding.UTF8);
            return MD5(input.ReadToEnd());
        }
    }
}
