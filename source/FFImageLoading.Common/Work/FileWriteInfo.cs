using System;

namespace FFImageLoading
{
    public readonly struct FileWriteInfo
    {
        public FileWriteInfo(string filePath, string sourcePath)
        {
            FilePath = filePath;
            SourcePath = sourcePath;
        }

        public readonly string SourcePath;

        public readonly string FilePath;
    }
}
