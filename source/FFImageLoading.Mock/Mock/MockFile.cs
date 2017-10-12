using System;

namespace FFImageLoading.Mock
{
    public class MockFile
    {
        public MockFile(byte[] data, string path)
        {
            Data = data;
            Path = path;
        }

        public byte[] Data { get; private set; }

        public string Path { get; private set; }
    }
}
