using System;

namespace FFImageLoading.Args
{
    [Preserve(AllMembers = true)]
    public class FileWriteFinishedEventArgs : EventArgs
    {
        public FileWriteFinishedEventArgs(FileWriteInfo fileWriteInfo)
        {
            FileWriteInfo = fileWriteInfo;
        }

        public FileWriteInfo FileWriteInfo { get; private set; }
    }
}
