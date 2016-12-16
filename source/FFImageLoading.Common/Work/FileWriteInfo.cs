using System;

namespace FFImageLoading
{
    public class FileWriteInfo
    {
        public FileWriteInfo(string filePath, string sourcePath)
        {
            FilePath = filePath;
            SourcePath = sourcePath;
        }

        public string SourcePath { get; private set; }

        public string FilePath { get; private set; }
    }
}
